using Fabula.Incrementant.Audio;
using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Sala Gotas de Orvalho (pedido do desenvolvedor em 2026-10-18). Em cada coluna pintada da sala, as operárias e as jardineiras
    /// cobrem de terra fofa (<see cref="CellMaterial.Spoil"/>, presa onde foi posta) a parte de cima
    /// (<see cref="ColonySettings.DewSoilShare"/>, 30%), de cima para baixo; depois põem uma folha
    /// (<see cref="CellMaterial.PlantLeaf"/>) logo abaixo da terra, a cada duas colunas. Com as duas etapas feitas, a cada
    /// <see cref="ColonySettings.DewDropSeconds"/> cai uma gota (uma célula de água) de uma coluna da sala; a água dentro da
    /// sala não é absorvida pelas paredes (<see cref="WaterSimulation"/>), então fica para as formigas regarem os canteiros.
    /// As etapas saem do estado da grade a cada rodada de campos: nada disto precisa ir para o save além da pintura da sala.
    /// </summary>
    public sealed partial class AntColony
    {
        FlowField _dewSoilField;
        // Campo até a queda das gotas (_dewDropCells): de onde a jardineira cuida da sala pronta.
        FlowField _dewTendField;
        // Jardineiras cuidando da sala neste passo, os blocos que cada uma escolheu (chave = bloco + 1) e os pontos de queda
        // que já têm jardineira ao lado (só eles aceleram).
        int _dewTendCount;
        int _dewTendedPoints;
        readonly HashSet<int> _dewTendTargets = new HashSet<int>();
        bool _dewFieldIdle = true;
        // Pontos de terra fofa e de folha ainda por pôr (índices de célula), mais os de onde cai a gota.
        readonly List<int> _dewSoilCells = new List<int>();
        readonly HashSet<int> _dewSoilSet = new HashSet<int>();
        readonly HashSet<int> _dewLeafSet = new HashSet<int>();
        readonly List<int> _dewLeafCells = new List<int>();
        readonly List<int> _dewDropCells = new List<int>();
        readonly List<int> _dewColumn = new List<int>();
        static readonly List<int> _dewNoCells = new List<int>();
        // Peças já postas pelas formigas (terra fofa e folhas), recontadas a cada rodada: entram na divisão com os canteiros.
        readonly List<int> _dewStoreSources = new List<int>();
        // Peças que a forma atual da sala não quer mais: a terra vai para _reclaimSoil (escavada e reaproveitada); a folha vira pixel
        // solto marcado, que as formigas levam ao ponto de folha certo. Enquanto houver sobras, a sala não solta gota.
        readonly List<int> _dewStaleLeaves = new List<int>();
        int _dewStale;
        int _dewSoilPlaced;
        int _dewLeavesPlaced;
        int _dewSoilSpots;
        int _dewLeafSpots;
        int _dewCarriers;
        bool _dewActive;
        // Pôs terra ou folha: os pontos e as etapas da sala pedem outra rodada (nem toda peça muda a caminhabilidade).
        bool _dewDirty;
        float _dewTimer;

        /// <summary>Pontos da sala onde ainda cabe terra fofa (etapa 1).</summary>
        public int DewSoilSpots => _dewSoilSpots;

        /// <summary>Pontos da sala onde ainda cabe uma folha (etapa 2; 0 enquanto falta terra fofa).</summary>
        public int DewLeafSpots => _dewLeafSpots;

        /// <summary>A sala terminou as duas etapas e solta gotas.</summary>
        public bool DewActive => _dewActive;

        /// <summary>Gotas que caíram da sala desde o começo do mapa (não vai para o save).</summary>
        public int DewDrops { get; private set; }

        /// <summary>Demanda de pixels de planta: canteiros esperando e pontos de folha da sala Gotas de Orvalho.</summary>
        int PlantDemand => _bedsAwaitingPlant + _dewLeafSpots;

        /// <summary>Quantas formigas levando terra a sala chama: uma a cada <see cref="ColonySettings.DewSpotsPerWorker"/> pontos, pelo menos uma.</summary>
        int WantedDewCarriers => Mathf.Max(1, (_dewSoilSpots + _settings.DewSpotsPerWorker - 1) / _settings.DewSpotsPerWorker);

        // ---------------------------------------------------------------- etapas

        /// <summary>
        /// Refaz os pontos da sala (terra fofa, folha, queda da gota) e o campo até a terra. Os pontos de folha entram em
        /// <paramref name="storeSources"/> (as células ao lado deles): a colheita e a entrega da planta usam o mesmo campo dos canteiros.
        /// </summary>
        void BuildDewSpots(List<int> storeSources)
        {
            _dewDirty = false;
            _dewSoilCells.Clear();
            _dewSoilSet.Clear();
            _dewLeafCells.Clear();
            _dewLeafSet.Clear();
            _dewDropCells.Clear();
            _dewSoilSpots = 0;
            _dewLeafSpots = 0;
            _dewSoilPlaced = 0;
            _dewLeavesPlaced = 0;
            _dewStale = 0;
            _dewStaleLeaves.Clear();
            _dewActive = false;
            if (_grid.DewRoomCount == 0)
            {
                if (!_dewFieldIdle)
                {
                    _dewSoilField.Invalidate();
                    _dewTendField.Invalidate();
                    _dewFieldIdle = true;
                }
                return;
            }
            _dewFieldIdle = false;
            _dewSoilField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _dewTendField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);

            int coveredCells = 0;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                int low = -1, high = -1;
                for (int cy = cyMin; cy <= cyMax; cy++)
                {
                    if (_grid.GetChunkDewRoomCount(cx, cy) == 0)
                        continue;
                    if (low < 0)
                        low = cy;
                    high = cy;
                }
                if (low < 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int yBottom = Mathf.Max(area.yMin, low << PixelGrid.ChunkShift);
                int yTop = Mathf.Min(area.yMax, (high + 1) << PixelGrid.ChunkShift) - 1;
                for (int x = x0; x < x1; x++)
                    coveredCells += ScanDewColumn(x, yBottom, yTop, storeSources);
            }

            _dewSoilSpots = _dewSoilCells.Count;
            // A folha só começa com a terra toda posta; antes disso os pontos dela ficam de fora.
            if (_dewSoilSpots > 0)
            {
                _dewLeafCells.Clear();
                _dewLeafSet.Clear();
            }
            _dewLeafSpots = _dewLeafCells.Count;
            for (int k = 0; k < _dewLeafCells.Count; k++)
                AddStandCells(_dewLeafCells[k] % _grid.Width, _dewLeafCells[k] / _grid.Width, storeSources);
            ReleaseStaleDewLeaves();
            _dewActive = _dewSoilSpots == 0 && _dewLeafSpots == 0 && _dewStale == 0 && coveredCells > 0;
            UpdateField(_dewSoilField, _dewSoilCells);
            UpdateField(_dewTendField, _dewActive ? _dewDropCells : _dewNoCells);
        }

        /// <summary>
        /// Folha fora do lugar (a sala cresceu ou encolheu): vira pixel solto marcado, que cai e é guardado de novo onde a folha
        /// é pedida. A terra fofa sobrada já foi para <see cref="_reclaimSoil"/> pela varredura da coluna.
        /// </summary>
        void ReleaseStaleDewLeaves()
        {
            if (_dewStaleLeaves.Count == 0)
                return;
            foreach (int i in _dewStaleLeaves)
            {
                int x = i % _grid.Width, y = i / _grid.Width;
                if (_grid.Get(x, y) != CellMaterial.PlantLeaf)
                    continue;
                byte shade = _grid.GetShade(x, y);
                // Passa por Empty: trocar planta por material não-planta guardaria a planta atrás da célula (PushBehindAt).
                _grid.Set(x, y, CellMaterial.Empty, 0);
                _grid.Set(x, y, CellMaterial.FallenLeaf, shade);
                _grid.SetMarked(x, y, true);
            }
            // O pixel solto cai e a sala é lida de novo: a folha não conta como posta nem a terra de baixo como coberta.
            _dewDirty = true;
        }

        /// <summary>
        /// Uma coluna da sala, de cima para baixo: a parte de cima (<see cref="ColonySettings.DewSoilShare"/> das células pintadas)
        /// recebe terra fofa, a célula logo abaixo recebe a folha (colunas pares) e a livre embaixo de tudo é de onde cai a gota.
        /// Devolve quantas células da parte de cima já têm material (a terra fofa posta ou a parede natural).
        /// </summary>
        int ScanDewColumn(int x, int yBottom, int yTop, List<int> storeSources)
        {
            _dewColumn.Clear();
            for (int y = yTop; y >= yBottom; y--)
            {
                if (_grid.IsDewRoom(x, y))
                    _dewColumn.Add(y);
            }
            int height = _dewColumn.Count;
            if (height < _settings.DewMinColumnHeight)
            {
                // Coluna curta demais para a sala: o que as formigas puseram nela não serve mais.
                CollectDewStale(x, 0, -1);
                return 0;
            }
            int band = Mathf.Clamp(Mathf.CeilToInt(height * _settings.DewSoilShare - 0.001f), 1, height - 1);
            int covered = 0;
            for (int j = 0; j < band; j++)
            {
                int y = _dewColumn[j];
                if (_grid.IsSolid(x, y))
                {
                    // Folha dentro da faixa de terra (a sala cresceu para baixo): sobra de uma forma antiga da sala.
                    if (_grid.Get(x, y) == CellMaterial.PlantLeaf)
                    {
                        _dewStaleLeaves.Add(_grid.ToIndex(x, y));
                        _dewStale++;
                        continue;
                    }
                    covered++;
                    if (_grid.Get(x, y) == CellMaterial.Spoil)
                        _dewSoilPlaced++;
                }
                else if (IsDewSoilSpot(x, y) && IsReachableFromHome(x, y))
                {
                    int i = _grid.ToIndex(x, y);
                    _dewSoilCells.Add(i);
                    _dewSoilSet.Add(i);
                }
            }

            int yBand = _dewColumn[band - 1];
            bool leafSlot = _dewColumn[band] == yBand - 1 && _grid.IsSolid(x, yBand);
            // Abaixo da faixa só cabe a folha do lugar dela; o resto (terra, outras folhas) sobrou de antes.
            CollectDewStale(x, band, leafSlot && (x & 1) == 0 ? yBand - 1 : -1);
            if (!leafSlot)
                return covered;
            int yDrop = yBand - 1;
            if ((x & 1) == 0)
            {
                if (_grid.Get(x, yDrop) == CellMaterial.PlantLeaf)
                {
                    _dewLeavesPlaced++;
                    yDrop--;
                }
                else if (IsDewLeafSpot(x, yDrop) && IsReachableFromHome(x, yDrop))
                {
                    int i = _grid.ToIndex(x, yDrop);
                    _dewLeafCells.Add(i);
                    _dewLeafSet.Add(i);
                }
            }
            if (_grid.IsDewRoom(x, yDrop) && !_grid.IsSolid(x, yDrop) && !_grid.HasWater(x, yDrop))
                _dewDropCells.Add(_grid.ToIndex(x, yDrop));
            return covered;
        }

        /// <summary>
        /// Da posição <paramref name="from"/> da coluna (<see cref="_dewColumn"/>, de cima para baixo) até o fim: terra fofa
        /// vira terra a recolher (<see cref="_reclaimSoil"/>) e folha fora de <paramref name="leafY"/> vira folha a soltar.
        /// </summary>
        void CollectDewStale(int x, int from, int leafY)
        {
            for (int j = from; j < _dewColumn.Count; j++)
            {
                int y = _dewColumn[j];
                CellMaterial material = _grid.Get(x, y);
                if (material == CellMaterial.Spoil)
                {
                    _reclaimSoil.Add(_grid.ToIndex(x, y));
                    _dewStale++;
                }
                else if (material == CellMaterial.PlantLeaf && y != leafY)
                {
                    _dewStaleLeaves.Add(_grid.ToIndex(x, y));
                    _dewStale++;
                }
            }
        }

        bool IsReachableFromHome(int x, int y) => _homeField.Get(x, y) != FlowField.Unreachable;

        /// <summary>
        /// Cabe terra fofa na célula: da sala, livre (sem material, nutriente nem água), pendurada em algo sólido por cima (a
        /// terra cobre o teto de cima para baixo), caminhável e sem fechar a passagem (<see cref="KeepsPassage"/>).
        /// </summary>
        bool IsDewSoilSpot(int x, int y) =>
            _grid.IsDewRoom(x, y) && !_grid.IsSolid(x, y) && _grid.IsSolid(x, y + 1) && !_grid.HasNutrient(x, y) &&
            !_grid.HasWater(x, y) && _walkability.IsWalkable(x, y) && KeepsPassage(x, y);

        /// <summary>Cabe a folha na célula: as mesmas condições da terra fofa (a folha fica pendurada embaixo dela).</summary>
        bool IsDewLeafSpot(int x, int y) => (x & 1) == 0 && IsDewSoilSpot(x, y);

        // ---------------------------------------------------------------- gota

        /// <summary>Conta o tempo da sala pronta e solta uma gota a cada <see cref="ColonySettings.DewDropSeconds"/>.</summary>
        void TickDew(float deltaTime)
        {
            if (!_dewActive)
            {
                _dewTimer = 0f;
                return;
            }
            // Os pontos de queda com jardineira ao lado somam DewTendBoost ao ritmo, na proporção da sala coberta (a gota cai mais cedo).
            float covered = _dewDropCells.Count > 0 ? Mathf.Min(1f, _dewTendedPoints / (float)_dewDropCells.Count) : 0f;
            _dewTimer += deltaTime * (1f + covered * _settings.DewTendBoost);
            if (_dewTimer < _settings.DewDropSeconds)
                return;
            // Cheia ou sem ponto livre, a gota espera o próximo intervalo.
            _dewTimer = 0f;
            if (CountDewWater() >= _settings.DewMaxRoomWater || _dewDropCells.Count == 0)
                return;
            int start = _rng.Next(_dewDropCells.Count);
            for (int k = 0; k < _dewDropCells.Count; k++)
            {
                int i = _dewDropCells[(start + k) % _dewDropCells.Count];
                int x = i % _grid.Width, y = i / _grid.Width;
                if (!_grid.IsDewRoom(x, y) || _grid.IsSolid(x, y) || _grid.HasWater(x, y) || !_grid.IsSolid(x, y + 1))
                    continue;
                _grid.SetWater(x, y, true);
                DewDrops++;
                return;
            }
        }

        // ---------------------------------------------------------------- cuidado da sala

        /// <summary>Blocos da sala: grupos de <see cref="ColonySettings.DewPointsPerTender"/> pontos de queda vizinhos (a lista vem em ordem de x).</summary>
        int DewBlockCount => (_dewDropCells.Count + _settings.DewPointsPerTender - 1) / _settings.DewPointsPerTender;

        /// <summary>Pontos de queda que o bloco <paramref name="block"/> cobre (o último grupo pode ser menor).</summary>
        int DewBlockSize(int block) => Mathf.Clamp(_dewDropCells.Count - block * _settings.DewPointsPerTender, 0, _settings.DewPointsPerTender);

        /// <summary>Ponto de queda do meio do bloco: onde a jardineira fica ao lado.</summary>
        Vector2Int DewBlockAnchor(int block)
        {
            int i = _dewDropCells[block * _settings.DewPointsPerTender + DewBlockSize(block) / 2];
            return new Vector2Int(i % _grid.Width, i / _grid.Width);
        }

        /// <summary>Bloco que outra jardineira ainda não escolheu (e, se possível, não o último que esta cuidou); chave = bloco + 1.</summary>
        bool PickDewBlock(Ant ant, out int block)
        {
            int count = DewBlockCount;
            block = -1;
            for (int pass = 0; pass < 2; pass++)
            {
                int seen = 0;
                for (int b = 0; b < count; b++)
                {
                    if (_dewTendTargets.Contains(b + 1) || pass < 1 && b + 1 == ant.LastBlock)
                        continue;
                    if (_rng.Next(++seen) == 0)
                        block = b;
                }
                if (seen > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Jardineira ociosa vai cuidar de um bloco da sala pronta: uma por bloco (<see cref="DewBlockCount"/>), cada uma
        /// com vários pontos de queda, como nos canteiros da sala de nutrientes.
        /// </summary>
        bool TryStartDewCare(Ant ant)
        {
            if (!_dewActive || _dewDropCells.Count == 0 || _dewTendCount >= DewBlockCount ||
                _dewTendField == null || _dewTendField.Get(ant.X, ant.Y) == FlowField.Unreachable ||
                !PickDewBlock(ant, out int block))
                return false;
            ant.State = AntState.TendingDew;
            ant.Interacting = false;
            ant.EggId = block + 1;
            _dewTendTargets.Add(ant.EggId);
            _dewTendCount++;
            UpdateTendingDew(ant);
            return true;
        }

        void LeaveDewCare(Ant ant)
        {
            ant.State = AntState.Idle;
            ant.EggId = 0;
            ant.Interacting = false;
            Hold(ant);
        }

        /// <summary>Vai até o bloco escolhido e fica ao lado (<see cref="Ant.Interacting"/>); sai se a sala deixa de precisar dela.</summary>
        void UpdateTendingDew(Ant ant)
        {
            if (!_dewActive || _dewDropCells.Count == 0 || _dewTendField == null ||
                _dewTendField.Get(ant.X, ant.Y) == FlowField.Unreachable || WantsTender(ant, _garden))
            {
                LeaveDewCare(ant);
                return;
            }
            // A sala encolheu e o bloco sumiu: escolhe outro.
            if (ant.EggId <= 0 || ant.EggId > DewBlockCount)
            {
                ant.Interacting = false;
                if (!PickDewBlock(ant, out int block))
                {
                    LeaveDewCare(ant);
                    return;
                }
                ant.EggId = block + 1;
                _dewTendTargets.Add(ant.EggId);
            }
            // Os pontos livres mudam com a água: o ponto de ficar é refeito a cada passo.
            Vector2Int cell = DewBlockAnchor(ant.EggId - 1);
            ant.TargetX = cell.x;
            ant.TargetY = cell.y;
            if (!NearCell(ant, cell))
            {
                ant.Interacting = false;
                WalkToCell(ant, cell, _dewTendField, _grid.IsDewRoom(ant.X, ant.Y));
                return;
            }
            ant.Interacting = true;
            if (cell.x != ant.X)
                ant.FacingX = cell.x > ant.X ? 1 : -1;
            ant.Cooldown = _settings.CarePauseSeconds;
        }

        int CountDewWater()
        {
            int count = 0;
            RectInt area = _grid.Active;
            for (int cy = area.yMin >> PixelGrid.ChunkShift; cy <= (area.yMax - 1) >> PixelGrid.ChunkShift; cy++)
            for (int cx = area.xMin >> PixelGrid.ChunkShift; cx <= (area.xMax - 1) >> PixelGrid.ChunkShift; cx++)
            {
                if (_grid.GetChunkDewRoomCount(cx, cy) == 0 || _grid.GetChunkWaterCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift), x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift), y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (_grid.IsDewRoom(x, y) && _grid.HasWater(x, y))
                        count++;
                }
            }
            return count;
        }

        // ---------------------------------------------------------------- terra fofa e folha

        /// <summary>Larga a terra fofa que leva num ponto vizinho da sala; sobrando terra, entrega o resto.</summary>
        bool TryPlaceDewSoil(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                int i = _grid.ToIndex(x, y);
                if (!_dewSoilSet.Contains(i) || !IsDewSoilSpot(x, y) || AntAt(x, y))
                    continue;
                _grid.Set(x, y, CellMaterial.Spoil, ant.CarriedShade);
                Emit(SfxId.Drop);
                _dewSoilSet.Remove(i);
                _dewSoilSpots = Mathf.Max(0, _dewSoilSpots - 1);
                _dewDirty = true;
                ant.DropTop();
                ant.DewJob = false;
                ant.Cooldown = _settings.DropSeconds;
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                if (ant.CarriedCount > 0)
                    StartDelivery(ant);
                else
                    ant.State = AntState.Idle;
                return true;
            }
            return false;
        }

        /// <summary>Pendura a folha que leva num ponto vizinho da sala (depois da terra fofa). Falso se não há ponto encostado.</summary>
        bool TryStoreDewLeaf(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                int i = _grid.ToIndex(x, y);
                if (!_dewLeafSet.Contains(i) || !IsDewLeafSpot(x, y) || AntAt(x, y))
                    continue;
                _grid.Set(x, y, CellMaterial.PlantLeaf, ant.CarriedShade);
                _dewLeafSet.Remove(i);
                _dewLeafSpots = Mathf.Max(0, _dewLeafSpots - 1);
                _dewDirty = true;
                _plantsInFlight = Mathf.Max(0, _plantsInFlight - 1);
                HarvestedPixels++;
                Emit(SfxId.Plant);
                ant.DropTop();
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                ant.State = ant.CarriedCount > 0 ? AntState.Carrying : AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                return true;
            }
            return false;
        }
    }
}
