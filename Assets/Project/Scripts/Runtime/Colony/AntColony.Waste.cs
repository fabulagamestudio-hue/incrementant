using Fabula.Incrementant.Audio;
using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Área de resíduos (pedido do desenvolvedor em 2026-10-18; design 21, R0): sala pintada, liberada depois do primeiro
    /// ataque de cupins. As formigas levam para lá o entulho das paredes e canteiros que os cupins destruíram e os restos
    /// dos cupins mortos, e o empilham do chão para cima (<see cref="CellMaterial.Refuse"/> e <see cref="CellMaterial.TermiteRemains"/>).
    /// Cada célula de resíduo se decompõe sozinha em <see cref="RaidSettings.WasteDecaySeconds"/>: o entulho some e libera
    /// espaço; o resto de cupim vira <see cref="RaidSettings.RemainsLeaves"/> folhas soltas dentro da área (pedido do
    /// desenvolvedor em 2026-10-19), que caem e são levadas aos canteiros. Sem área (ou cheia, ou sem caminho), o entulho
    /// vai aos canteiros ou ao despejo como terra, e os restos ao despejo.
    /// </summary>
    public sealed partial class AntColony
    {
        FlowField _wasteField;
        bool _wasteFieldIdle = true;
        int _wasteSpots;
        int _wasteCarriers;
        // Células de resíduo e o tempo que falta para cada uma se decompor (refeito pela grade ao carregar).
        readonly List<int> _wasteCells = new List<int>();
        readonly List<float> _wasteTimes = new List<float>();

        /// <summary>Células da área de resíduos onde ainda cabe um resíduo (chão livre, caminhável, sem fechar passagem).</summary>
        public int WasteSpots => _wasteSpots;

        /// <summary>Células de resíduo se decompondo.</summary>
        public int WasteCells => _wasteCells.Count;

        /// <summary>Resíduos (entulho e restos) que as formigas depositaram na área desde o começo do mapa (não vai para o save).</summary>
        public int WasteDeposited { get; private set; }

        // Até onde (em xadrez) o resto que se decompõe procura células vazias da área para as folhas.
        const int RemainsLeafReach = 12;
        // Sem lugar para todas as folhas, o resto espera este tempo e tenta de novo (o espaço abre conforme o resíduo some).
        const float RemainsRetrySeconds = 1f;
        readonly List<int> _leafSpots = new List<int>();

        /// <summary>
        /// Decompõe os resíduos quando o tempo de cada um acaba: o entulho some; o resto de cupim vira folhas soltas na área
        /// (<see cref="TryRotRemains"/>).
        /// </summary>
        void TickWaste(float deltaTime)
        {
            int w = _grid.Width;
            for (int i = _wasteCells.Count - 1; i >= 0; i--)
            {
                _wasteTimes[i] -= deltaTime;
                if (_wasteTimes[i] > 0f)
                    continue;
                int cell = _wasteCells[i];
                int x = cell % w, y = cell / w;
                CellMaterial material = _grid.Get(x, y);
                if (material == CellMaterial.TermiteRemains && !TryRotRemains(x, y))
                {
                    _wasteTimes[i] = RemainsRetrySeconds;
                    continue;
                }
                if (material == CellMaterial.Refuse)
                    _grid.Set(x, y, CellMaterial.Empty, 0);
                int last = _wasteCells.Count - 1;
                _wasteCells[i] = _wasteCells[last];
                _wasteTimes[i] = _wasteTimes[last];
                _wasteCells.RemoveAt(last);
                _wasteTimes.RemoveAt(last);
            }
        }

        /// <summary>
        /// O resto de cupim em (<paramref name="x"/>, <paramref name="y"/>) vira <see cref="RaidSettings.RemainsLeaves"/> folhas
        /// soltas: a própria célula e as vazias mais próximas da área de resíduos (sem água nem formiga). Cada folha é um
        /// <see cref="CellMaterial.FallenLeaf"/> marcado, que cai como grão e é colhido para os canteiros como a folha do corpo.
        /// Sem lugar para todas, nada muda e devolve false (tenta de novo depois).
        /// </summary>
        bool TryRotRemains(int x, int y)
        {
            int leaves = RaidSettings.RemainsLeaves;
            _leafSpots.Clear();
            if (leaves > 0)
                _leafSpots.Add(_grid.ToIndex(x, y));
            for (int r = 1; r <= RemainsLeafReach && _leafSpots.Count < leaves; r++)
            for (int dy = r; dy >= -r && _leafSpots.Count < leaves; dy--)
            for (int dx = -r; dx <= r && _leafSpots.Count < leaves; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
                    continue;
                int lx = x + dx, ly = y + dy;
                if (_grid.InBounds(lx, ly) && IsLeafSpot(lx, ly))
                    _leafSpots.Add(_grid.ToIndex(lx, ly));
            }
            if (_leafSpots.Count < leaves)
                return false;

            _grid.Set(x, y, CellMaterial.Empty, 0);
            int w = _grid.Width;
            foreach (int cell in _leafSpots)
            {
                int lx = cell % w, ly = cell / w;
                // Tom variado e determinístico (como a folha da planta), para a pilha não sair chapada.
                _grid.Set(lx, ly, CellMaterial.FallenLeaf, (byte)(104 + (cell * 37 & 47)));
                _grid.SetMarked(lx, ly, true);
            }
            RemainsRotted++;
            return true;
        }

        /// <summary>Célula vazia da área de resíduos onde uma folha do resto pode surgir (sem água nem formiga em cima).</summary>
        bool IsLeafSpot(int x, int y) =>
            _grid.IsWasteRoom(x, y) && _grid.Get(x, y) == CellMaterial.Empty && !_grid.HasWater(x, y) &&
            !_grid.HasNutrient(x, y) && !AntAt(x, y);

        /// <summary>Restos de cupim que já viraram folhas na área de resíduos desde o começo do mapa (não vai para o save).</summary>
        public int RemainsRotted { get; private set; }

        /// <summary>Volta a decompor os resíduos que vieram do save (o tempo recomeça cheio).</summary>
        void RescanWaste()
        {
            _wasteCells.Clear();
            _wasteTimes.Clear();
            RectInt area = _grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                if (!CellMaterials.IsDepositedWaste(_grid.Get(x, y)))
                    continue;
                _wasteCells.Add(_grid.ToIndex(x, y));
                _wasteTimes.Add(RaidSettings.WasteDecaySeconds);
            }
        }

        /// <summary>Campo até os pontos livres da área de resíduos (só existe com área pintada).</summary>
        void BuildWasteField()
        {
            _wasteSpots = 0;
            if (_grid.WasteRoomCount == 0)
            {
                if (!_wasteFieldIdle)
                {
                    _wasteField.Invalidate();
                    _wasteFieldIdle = true;
                }
                return;
            }
            _wasteFieldIdle = false;
            _wasteField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _sources.Clear();
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkWasteRoomCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (IsWasteSpot(x, y))
                        _sources.Add(_grid.ToIndex(x, y));
                }
            }
            _wasteSpots = _sources.Count;
            UpdateField(_wasteField, _sources);
            _sources.Clear();
        }

        /// <summary>
        /// Cabe um resíduo na célula: na área de resíduos, livre (sem material, nutriente nem água), caminhável, apoiada em
        /// algo sólido embaixo (a pilha cresce do chão) e sem fechar a passagem (<see cref="KeepsPassage"/>).
        /// </summary>
        bool IsWasteSpot(int x, int y) =>
            _grid.IsWasteRoom(x, y) && !_grid.IsSolid(x, y) && _grid.IsSolid(x, y - 1) && !_grid.HasNutrient(x, y) &&
            !_grid.HasWater(x, y) && _walkability.IsWalkable(x, y) && KeepsPassage(x, y);

        /// <summary>Entulho ou restos na mão e área de resíduos com lugar e caminho: leva para lá.</summary>
        bool TryStartWaste(Ant ant)
        {
            if (!CellMaterials.IsWaste(ant.Carried) || _wasteSpots == 0 || _wasteField == null ||
                _wasteField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ReleaseDumpSide(ant);
            ant.State = AntState.CarryingWaste;
            _wasteCarriers++;
            return true;
        }

        /// <summary>Desce o campo dos pontos livres e larga o resíduo num ponto vizinho. Sem caminho, entrega como terra.</summary>
        void UpdateCarryingWaste(Ant ant)
        {
            int d = _wasteField != null && _wasteSpots > 0 ? _wasteField.Get(ant.X, ant.Y) : FlowField.Unreachable;
            if (d == FlowField.Unreachable)
            {
                DeliverWithoutWasteRoom(ant);
                return;
            }
            if (d <= 1 && TryPlaceWaste(ant))
                return;
            if (d > 0 && StepDownhill(ant, _wasteField))
                return;
            if (d == 0)
            {
                // Em cima do ponto (o resíduo não cai na própria formiga): sai para o lado e larga dali.
                Wander(ant);
                return;
            }
            DeliverWithoutWasteRoom(ant);
        }

        void DeliverWithoutWasteRoom(Ant ant)
        {
            ant.State = AntState.Carrying;
            if (!TryStartBedSoil(ant))
                AssignDumpSide(ant);
        }

        bool TryPlaceWaste(Ant ant)
        {
            for (int k = 0; k < 8; k++)
            {
                int x = ant.X + AntNavigation.Dx[k], y = ant.Y + AntNavigation.Dy[k];
                if (!IsWasteSpot(x, y) || AntAt(x, y))
                    continue;
                // O resto de cupim fica como resto (vira folhas ao se decompor); o entulho vira resíduo comum.
                CellMaterial deposited = ant.Carried == CellMaterial.TermiteRemains ? CellMaterial.TermiteRemains : CellMaterial.Refuse;
                _grid.Set(x, y, deposited, ant.CarriedShade);
                Emit(SfxId.Drop);
                _wasteCells.Add(_grid.ToIndex(x, y));
                _wasteTimes.Add(RaidSettings.WasteDecaySeconds);
                WasteDeposited++;
                ant.DropTop();
                ant.Cooldown = _settings.DropSeconds;
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                // Sobrou carga: o próximo pixel do mesmo tipo vai para outro ponto; se for terra comum, segue a entrega normal.
                if (ant.CarriedCount == 0)
                    ant.State = AntState.Idle;
                else if (!CellMaterials.IsWaste(ant.Carried))
                    StartDelivery(ant);
                return true;
            }
            return false;
        }
    }
}
