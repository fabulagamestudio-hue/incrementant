using Fabula.Incrementant.Audio;
using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Área de resíduos (pedido do desenvolvedor em 2026-10-18; design 21, R0): sala pintada, liberada depois do primeiro
    /// ataque de cupins. As formigas levam para lá o entulho das paredes e canteiros que os cupins destruíram e os restos
    /// dos cupins mortos. Desde 2026-10-19 o resíduo é um item guardado (<see cref="PixelGrid.GetStored"/>, como nos
    /// depósitos): não bloqueia a passagem e pode ocupar qualquer célula vazia da área, então ela enche inteira sem atrapalhar.
    /// Cada resíduo se decompõe sozinho em <see cref="RaidSettings.WasteDecaySeconds"/>: o entulho some; o resto de cupim vira
    /// <see cref="RaidSettings.RemainsLeaves"/> folhas guardadas na área (pedido do desenvolvedor em 2026-10-19), soltas fora do
    /// depósito de alimentos, que as formigas levam a ele (ou aos canteiros). Sem área (ou cheia, ou sem caminho), o entulho
    /// vai como terra (depósito de construção, canteiros ou despejo) e os restos ao despejo.
    /// </summary>
    public sealed partial class AntColony
    {
        FlowField _wasteField;
        bool _wasteFieldIdle = true;
        int _wasteSpots;
        int _wasteCarriers;
        // Células com resíduo guardado e o tempo que falta para cada uma se decompor (refeito pela grade ao carregar).
        readonly List<int> _wasteCells = new List<int>();
        readonly List<float> _wasteTimes = new List<float>();

        /// <summary>Células da área de resíduos onde ainda cabe um resíduo (vazias, sem item, caminháveis).</summary>
        public int WasteSpots => _wasteSpots;

        /// <summary>Resíduos se decompondo.</summary>
        public int WasteCells => _wasteCells.Count;

        /// <summary>Resíduos (entulho e restos) que as formigas depositaram na área desde o começo do mapa (não vai para o save).</summary>
        public int WasteDeposited { get; private set; }

        // Até onde (em xadrez) o resto que se decompõe procura células livres da área para as folhas.
        const int RemainsLeafReach = 12;
        // Sem lugar para todas as folhas, o resto espera este tempo e tenta de novo (o espaço abre conforme o resíduo some).
        const float RemainsRetrySeconds = 1f;
        readonly List<int> _leafSpots = new List<int>();

        /// <summary>
        /// Decompõe os resíduos quando o tempo de cada um acaba: o entulho some; o resto de cupim vira folhas guardadas na
        /// área (<see cref="TryRotRemains"/>).
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
                CellMaterial material = StoredItems.MaterialOf(_grid.GetStored(x, y));
                if (material == CellMaterial.TermiteRemains && !TryRotRemains(x, y))
                {
                    _wasteTimes[i] = RemainsRetrySeconds;
                    continue;
                }
                if (material == CellMaterial.Refuse)
                    _grid.SetStored(x, y, StoredItems.None);
                int last = _wasteCells.Count - 1;
                _wasteCells[i] = _wasteCells[last];
                _wasteTimes[i] = _wasteTimes[last];
                _wasteCells.RemoveAt(last);
                _wasteTimes.RemoveAt(last);
            }
        }

        /// <summary>
        /// O resto de cupim em (<paramref name="x"/>, <paramref name="y"/>) vira <see cref="RaidSettings.RemainsLeaves"/> folhas
        /// guardadas: a própria célula e as livres mais próximas da área de resíduos. Cada folha é um item de planta solto
        /// (fora do depósito de alimentos), que as formigas levam ao depósito ou aos canteiros. Sem lugar para todas, nada
        /// muda e devolve false (tenta de novo depois).
        /// </summary>
        bool TryRotRemains(int x, int y)
        {
            int leaves = RaidSettings.RemainsLeaves;
            _leafSpots.Clear();
            for (int r = 1; r <= RemainsLeafReach && _leafSpots.Count < leaves - 1; r++)
            for (int dy = r; dy >= -r && _leafSpots.Count < leaves - 1; dy--)
            for (int dx = -r; dx <= r && _leafSpots.Count < leaves - 1; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
                    continue;
                int lx = x + dx, ly = y + dy;
                if (_grid.InBounds(lx, ly) && IsLeafSpot(lx, ly))
                    _leafSpots.Add(_grid.ToIndex(lx, ly));
            }
            if (leaves > 0 && _leafSpots.Count < leaves - 1)
                return false;

            _grid.SetStored(x, y, StoredItems.None);
            if (leaves > 0)
                _leafSpots.Add(_grid.ToIndex(x, y));
            int w = _grid.Width;
            foreach (int cell in _leafSpots)
            {
                int lx = cell % w, ly = cell / w;
                // Tom variado e determinístico (como a folha da planta), para a pilha não sair chapada.
                _grid.SetStored(lx, ly, StoredItems.Of(CellMaterial.PlantLeaf), (byte)(104 + (cell * 37 & 47)));
            }
            RemainsRotted++;
            return true;
        }

        /// <summary>Célula livre da área de resíduos onde uma folha do resto pode ficar (sem material, item, nutriente nem água).</summary>
        bool IsLeafSpot(int x, int y) =>
            _grid.IsWasteRoom(x, y) && _grid.Get(x, y) == CellMaterial.Empty && _grid.GetStored(x, y) == StoredItems.None &&
            !_grid.HasWater(x, y) && !_grid.HasNutrient(x, y);

        /// <summary>Restos de cupim que já viraram folhas na área de resíduos desde o começo do mapa (não vai para o save).</summary>
        public int RemainsRotted { get; private set; }

        /// <summary>
        /// Volta a decompor os resíduos que vieram do save (o tempo recomeça cheio). Saves de antes de 2026-10-19 guardavam o
        /// resíduo como material (célula sólida): ele vira item guardado na mesma célula.
        /// </summary>
        void RescanWaste()
        {
            _wasteCells.Clear();
            _wasteTimes.Clear();
            RectInt area = _grid.Active;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                CellMaterial material = _grid.Get(x, y);
                if (CellMaterials.IsDepositedWaste(material))
                {
                    byte shade = _grid.GetShade(x, y);
                    _grid.Set(x, y, CellMaterial.Empty, 0);
                    _grid.SetStored(x, y, StoredItems.Of(material), shade);
                }
                if (StoredItems.KindOf(_grid.GetStored(x, y)) != StorageKind.Waste)
                    continue;
                _wasteCells.Add(_grid.ToIndex(x, y));
                _wasteTimes.Add(RaidSettings.WasteDecaySeconds * Effects.WasteTime);
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
        /// Cabe um resíduo na célula: na área de resíduos, livre (sem material, item, nutriente nem água) e caminhável. O
        /// resíduo guardado não bloqueia a passagem, então qualquer célula vazia da área serve (não só o chão).
        /// </summary>
        bool IsWasteSpot(int x, int y) =>
            _grid.IsWasteRoom(x, y) && !_grid.IsSolid(x, y) && _grid.GetStored(x, y) == StoredItems.None &&
            !_grid.HasNutrient(x, y) && !_grid.HasWater(x, y) && _walkability.IsWalkable(x, y);

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

        /// <summary>Desce o campo dos pontos livres e guarda o resíduo na própria célula ou numa vizinha. Sem caminho, entrega como terra.</summary>
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
            // No ponto, mas outra formiga já o ocupou: espera o campo ser refeito.
            Hold(ant);
        }

        void DeliverWithoutWasteRoom(Ant ant)
        {
            ant.State = AntState.Carrying;
            ant.StoreTarget = StorageKind.None;
            if (BlockDepot ? IsBlockItem(ant.Carried) && TryStartStock(ant, StorageKind.Blocks) : TryStartBedSoil(ant))
                return;
            AssignDumpSide(ant);
        }

        bool TryPlaceWaste(Ant ant)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                if (!IsWasteSpot(x, y))
                    continue;
                // O resto de cupim fica como resto (vira folhas ao se decompor); o entulho vira resíduo comum.
                CellMaterial deposited = ant.Carried == CellMaterial.TermiteRemains ? CellMaterial.TermiteRemains : CellMaterial.Refuse;
                _grid.SetStored(x, y, StoredItems.Of(deposited), ant.CarriedShade);
                Emit(SfxId.Drop, x, y);
                _wasteCells.Add(_grid.ToIndex(x, y));
                _wasteTimes.Add(RaidSettings.WasteDecaySeconds * Effects.WasteTime);
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
