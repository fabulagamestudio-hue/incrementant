using System;
using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Formigas, navegação e economia da colônia. Ciclo: ociosa → vai até um pixel marcado → cava →
    /// carrega até a faixa de despejo na superfície → solta o pixel (vira material granular) → ociosa.
    /// </summary>
    public sealed class AntColony
    {
        readonly PixelGrid _grid;
        readonly ColonySettings _settings;
        readonly TerrainLayout _layout;
        readonly System.Random _rng;
        readonly List<Ant> _ants = new List<Ant>();
        readonly List<int> _sources = new List<int>();
        readonly FlowField _digField;
        readonly FlowField _dumpField;
        readonly FlowField _homeField;
        readonly WalkabilityMap _walkability;
        readonly int[] _candidates = new int[8];

        int _builtStructureVersion = -1;
        int _builtMarkVersion = -1;
        float _fieldTimer;

        public IReadOnlyList<Ant> Ants => _ants;
        public int StoredPixels { get; private set; }
        public int TotalDelivered { get; private set; }
        public TerrainLayout Layout => _layout;

        /// <summary>Tempo simulado acumulado (s). Referência para <see cref="Ant.MoveStart"/>.</summary>
        public float SimulationTime { get; private set; }

        public int NextAntCost =>
            Mathf.RoundToInt(_settings.AntBaseCost *
                             Mathf.Pow(_settings.AntCostGrowth, Mathf.Max(0, _ants.Count - _settings.InitialAnts)));

        public AntColony(PixelGrid grid, ColonySettings settings, TerrainLayout layout, int seed)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _layout = layout;
            _rng = new System.Random(seed);
            _digField = new FlowField(grid.Width, grid.Height);
            _dumpField = new FlowField(grid.Width, grid.Height);
            _homeField = new FlowField(grid.Width, grid.Height);
            _walkability = new WalkabilityMap(grid);

            for (int i = 0; i < settings.InitialAnts; i++)
                Spawn();
        }

        public Ant Spawn()
        {
            var ant = new Ant { X = _layout.Nest.x, Y = _layout.Nest.y, FacingX = _rng.Next(2) == 0 ? -1 : 1 };
            PushOutOfSolid(ant);
            SnapVisual(ant);
            _ants.Add(ant);
            return ant;
        }

        public bool TryBuyAnt()
        {
            int cost = NextAntCost;
            if (StoredPixels < cost || _ants.Count >= _settings.MaxAnts)
                return false;
            StoredPixels -= cost;
            Spawn();
            return true;
        }

        public void Tick(float deltaTime)
        {
            SimulationTime += deltaTime;
            RefreshFields(deltaTime);
            for (int i = 0; i < _ants.Count; i++)
                UpdateAnt(_ants[i], deltaTime);
        }

        // ---------------------------------------------------------------- navegação

        void RefreshFields(float deltaTime)
        {
            _fieldTimer -= deltaTime;
            bool structureChanged = _grid.StructureVersion != _builtStructureVersion;
            bool marksChanged = _grid.MarkVersion != _builtMarkVersion;
            if (!structureChanged && !marksChanged)
                return;
            // Areia escorrendo muda a estrutura quase todo passo; o intervalo limita o custo dos BFS.
            if (_fieldTimer > 0f && _builtStructureVersion >= 0)
                return;

            _fieldTimer = _settings.FieldRebuildInterval;
            _walkability.Refresh();
            if (structureChanged)
            {
                BuildDumpField();
                BuildHomeField();
            }
            BuildDigField();
            _builtStructureVersion = _grid.StructureVersion;
            _builtMarkVersion = _grid.MarkVersion;
        }

        void BuildDigField()
        {
            _sources.Clear();
            if (_grid.MarkedCount > 0)
            {
                // Origens = células caminháveis com algum vizinho marcado. Procura pelo lado caminhável (poucas células,
                // só nos blocos com marca ou encostados neles), não pelo marcado (a área marcada costuma ser grande e
                // quase toda maciça). A distância do BFS não depende da ordem das origens, então o campo é o mesmo.
                int w = _grid.Width;
                RectInt area = _grid.Active;
                int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
                int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
                for (int cy = cyMin; cy <= cyMax; cy++)
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    if (!HasMarksAround(cx, cy))
                        continue;
                    int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                    int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                    int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                    int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        if (!_walkability.IsWalkableAt(y * w + x))
                            continue;
                        for (int k = 0; k < 8; k++)
                        {
                            if (!_grid.IsMarked(x + AntNavigation.Dx[k], y + AntNavigation.Dy[k]))
                                continue;
                            _sources.Add(y * w + x);
                            break;
                        }
                    }
                }
            }
            _digField.Build(_walkability, _sources);
        }

        /// <summary>O bloco ou algum dos 8 vizinhos tem marca (uma célula caminhável nele pode encostar numa marca).</summary>
        bool HasMarksAround(int cx, int cy)
        {
            for (int y = Mathf.Max(0, cy - 1); y <= Mathf.Min(_grid.ChunkRows - 1, cy + 1); y++)
            for (int x = Mathf.Max(0, cx - 1); x <= Mathf.Min(_grid.ChunkColumns - 1, cx + 1); x++)
            {
                if (_grid.GetChunkMarkedCount(x, y) > 0)
                    return true;
            }
            return false;
        }

        void BuildDumpField()
        {
            _sources.Clear();
            for (int x = _layout.DumpMinX; x <= _layout.DumpMaxX; x++)
            for (int y = Mathf.Max(0, _grid.GetSurfaceY(x) + 1); y < _grid.Height; y++)
            {
                if (_walkability.IsWalkable(x, y))
                    _sources.Add(_grid.ToIndex(x, y));
            }
            _dumpField.Build(_walkability, _sources);
        }

        void BuildHomeField()
        {
            _sources.Clear();
            for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
            {
                int x = _layout.Nest.x + dx;
                int y = _layout.Nest.y + dy;
                if (_walkability.IsWalkable(x, y))
                    _sources.Add(_grid.ToIndex(x, y));
            }
            _homeField.Build(_walkability, _sources);
        }

        // ---------------------------------------------------------------- comportamento

        void UpdateAnt(Ant ant, float deltaTime)
        {
            // Material granular pode cair por cima da formiga: ela sobe até achar espaço.
            if (_grid.IsSolid(ant.X, ant.Y))
                PushOutOfSolid(ant);

            // Perdeu o apoio (ex.: cavou o próprio piso): agarra a superfície vizinha se houver;
            // só cai, uma célula por passo, quando não há nada ao alcance.
            if (!AntNavigation.IsWalkable(_grid, ant.X, ant.Y))
            {
                if (TryCling(ant))
                    return;
                if (!_grid.IsSolid(ant.X, ant.Y - 1))
                {
                    BeginMove(ant, deltaTime);
                    ant.Y--;
                }
                return;
            }

            ant.Cooldown -= deltaTime;
            if (ant.Cooldown > 0f)
                return;

            switch (ant.State)
            {
                case AntState.Carrying:
                    UpdateCarrying(ant);
                    break;
                case AntState.Digging:
                    FinishDig(ant);
                    break;
                default:
                    UpdateIdle(ant);
                    break;
            }
        }

        void UpdateIdle(Ant ant)
        {
            if (_grid.MarkedCount > 0)
            {
                int d = _digField.Get(ant.X, ant.Y);
                if (d == 0 && TryStartDig(ant))
                    return;
                if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, _digField))
                    return;
            }

            int home = _homeField.Get(ant.X, ant.Y);
            if (home != FlowField.Unreachable && home > _settings.IdleHomeRadius && StepDownhill(ant, _homeField))
                return;
            Wander(ant);
        }

        bool TryStartDig(Ant ant)
        {
            int count = 0;
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (_grid.IsMarked(nx, ny) && CellMaterials.IsDiggable(_grid.Get(nx, ny)))
                    _candidates[count++] = k;
            }
            if (count == 0)
                return false;

            int pick = _candidates[_rng.Next(count)];
            ant.DigX = ant.X + AntNavigation.Dx[pick];
            ant.DigY = ant.Y + AntNavigation.Dy[pick];
            if (ant.DigX != ant.X)
                ant.FacingX = ant.DigX > ant.X ? 1 : -1;
            ant.State = AntState.Digging;
            ant.Cooldown = CellMaterials.DigSeconds(_grid.Get(ant.DigX, ant.DigY)) * _settings.DigTimeMultiplier;
            return true;
        }

        void FinishDig(Ant ant)
        {
            ant.State = AntState.Idle;
            bool adjacent = Mathf.Abs(ant.DigX - ant.X) <= 1 && Mathf.Abs(ant.DigY - ant.Y) <= 1;
            CellMaterial material = _grid.Get(ant.DigX, ant.DigY);
            // Outra formiga pode ter levado o pixel, ou ele caiu/foi desmarcado durante a escavação.
            if (!adjacent || !_grid.IsMarked(ant.DigX, ant.DigY) || !CellMaterials.IsDiggable(material))
                return;

            ant.Carried = material;
            ant.CarriedShade = _grid.GetShade(ant.DigX, ant.DigY);
            _grid.Set(ant.DigX, ant.DigY, CellMaterial.Empty, 0);
            ant.State = AntState.Carrying;
            ant.DumpTargetX = _rng.Next(_layout.DumpMinX, _layout.DumpMaxX + 1);
        }

        void UpdateCarrying(Ant ant)
        {
            int d = _dumpField.Get(ant.X, ant.Y);
            if (d == FlowField.Unreachable)
            {
                Wander(ant);
                return;
            }
            if (d > 0)
            {
                if (!StepDownhill(ant, _dumpField))
                    Wander(ant);
                return;
            }

            // Dentro da faixa de despejo: anda até o X sorteado para espalhar a pilha.
            if (ant.X != ant.DumpTargetX && StepTowardsDumpTarget(ant))
                return;
            if (!TryDrop(ant))
                Wander(ant);
        }

        bool StepTowardsDumpTarget(Ant ant)
        {
            int bestK = -1;
            int bestDistance = Mathf.Abs(ant.X - ant.DumpTargetX);
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (_dumpField.Get(nx, ny) != 0 || !AntNavigation.IsWalkable(_grid, nx, ny))
                    continue;
                int distance = Mathf.Abs(nx - ant.DumpTargetX);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestK = k;
                }
            }
            if (bestK < 0)
                return false;
            MoveTo(ant, bestK);
            return true;
        }

        bool TryDrop(Ant ant)
        {
            for (int i = 0; i < 3; i++)
            {
                int x = ant.X + (i == 0 ? 0 : i == 1 ? -1 : 1);
                int y = ant.Y + 1;
                if (!_grid.InBounds(x, y) || _grid.IsSolid(x, y))
                    continue;

                _grid.Set(x, y, CellMaterials.ToDumped(ant.Carried), ant.CarriedShade);
                ant.Carried = CellMaterial.Empty;
                ant.State = AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                StoredPixels++;
                TotalDelivered++;
                return true;
            }
            return false;
        }

        /// <summary>Anda para o vizinho caminhável de menor distância no campo. Empates são sorteados.</summary>
        bool StepDownhill(Ant ant, FlowField field)
        {
            int current = field.Get(ant.X, ant.Y);
            int best = current;
            int ties = 0;
            int chosen = -1;
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                int d = field.Get(nx, ny);
                if (d > best || d == FlowField.Unreachable || !AntNavigation.IsWalkable(_grid, nx, ny))
                    continue;
                if (d < best)
                {
                    best = d;
                    chosen = k;
                    ties = 1;
                }
                else if (d < current && _rng.Next(++ties) == 0)
                {
                    chosen = k;
                }
            }
            if (chosen < 0)
                return false;
            MoveTo(ant, chosen);
            return true;
        }

        void Wander(Ant ant)
        {
            int forward = 0;
            int any = 0;
            Span<int> forwardMoves = stackalloc int[8];
            for (int k = 0; k < 8; k++)
            {
                if (!AntNavigation.IsWalkable(_grid, ant.X + AntNavigation.Dx[k], ant.Y + AntNavigation.Dy[k]))
                    continue;
                _candidates[any++] = k;
                if (AntNavigation.Dx[k] == ant.FacingX)
                    forwardMoves[forward++] = k;
            }

            if (any == 0)
            {
                ant.Cooldown = 1f / _settings.AntSpeed;
                return;
            }
            // Prefere seguir em frente; às vezes (ou sem saída à frente) muda de rumo.
            int pick = forward > 0 && _rng.NextDouble() < 0.85
                ? forwardMoves[_rng.Next(forward)]
                : _candidates[_rng.Next(any)];
            MoveTo(ant, pick);
        }

        void MoveTo(Ant ant, int direction)
        {
            int dx = AntNavigation.Dx[direction];
            int dy = AntNavigation.Dy[direction];
            float step = dx != 0 && dy != 0 ? 1.4142f : 1f;
            BeginMove(ant, step / _settings.AntSpeed);
            ant.X += dx;
            ant.Y += dy;
            if (dx != 0)
                ant.FacingX = dx;
            ant.Cooldown += step / _settings.AntSpeed;
            if (ant.Cooldown < 0f)
                ant.Cooldown = 0f;
        }

        void PushOutOfSolid(Ant ant)
        {
            int y = ant.Y;
            while (y < _grid.Height - 1 && _grid.IsSolid(ant.X, y))
                y++;
            if (y != ant.Y)
            {
                ant.Y = y;
                SnapVisual(ant);
            }
        }

        // Ordem de preferência ao se agarrar: descer encostado (diagonais de baixo), lados, baixo, diagonais de cima, cima.
        // Índices de AntNavigation.Dx/Dy; a versão "Left" espelha para quem está virado para a esquerda.
        static readonly int[] ClingOrderRight = { 5, 7, 0, 1, 3, 4, 6, 2 };
        static readonly int[] ClingOrderLeft = { 7, 5, 1, 0, 3, 6, 4, 2 };

        bool TryCling(Ant ant)
        {
            int[] order = ant.FacingX >= 0 ? ClingOrderRight : ClingOrderLeft;
            foreach (int k in order)
            {
                if (!AntNavigation.IsWalkable(_grid, ant.X + AntNavigation.Dx[k], ant.Y + AntNavigation.Dy[k]))
                    continue;
                MoveTo(ant, k);
                return true;
            }
            return false;
        }

        void BeginMove(Ant ant, float duration)
        {
            ant.FromX = ant.X;
            ant.FromY = ant.Y;
            ant.MoveStart = SimulationTime;
            ant.MoveDuration = duration;
        }

        static void SnapVisual(Ant ant)
        {
            ant.FromX = ant.X;
            ant.FromY = ant.Y;
            ant.MoveDuration = 0f;
        }
    }
}
