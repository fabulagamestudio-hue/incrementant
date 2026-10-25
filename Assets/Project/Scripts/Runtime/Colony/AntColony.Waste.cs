using Fabula.Incrementant.Juice;
using Fabula.Incrementant.Audio;
using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Modo de conversão de uma área de resíduos (pedidos do desenvolvedor de 2026-10-20, KI-042 e KI-045). Desde 2026-10-21
    /// cada área tem o seu (<see cref="PixelGrid.GetWasteKind"/>, posto pelo pincel) e recebe só o conteúdo dele, sem se
    /// misturar com as outras. Valor gravado no save: não renumerar.
    /// </summary>
    public enum WasteMode : byte
    {
        /// <summary>Recebe entulho e restos de cupim: o entulho some e o resto vira folhas (soltas, levadas ao depósito de alimentos ou aos canteiros).</summary>
        RemainsToLeaves = 0,
        /// <summary>Recebe só restos de cupim: as folhas deles ficam na área e viram água (1 folha → 1 água).</summary>
        LeavesToWater = 1,
        /// <summary>Recebe só terra (a cavada que iria ao despejo e a da pilha de fora): vira areia e água (1 terra → 1 areia + 1 água).</summary>
        DirtToWater = 2,
        /// <summary>Recebe as folhas das plantas cortadas: cada folha vira um nutriente depois de um tempo (1 folha → 1 nutriente, pedido de 2026-10-24).</summary>
        LeavesToNutrient = 3,
    }

    /// <summary>
    /// Área de resíduos (pedido do desenvolvedor em 2026-10-18; design 21, R0): sala pintada, liberada depois do primeiro
    /// ataque de cupins. Desde 2026-10-19 o resíduo é um item guardado (<see cref="PixelGrid.GetStored"/>, como nos
    /// depósitos): não bloqueia a passagem e pode ocupar qualquer célula vazia da área. Cada célula da área tem um
    /// <see cref="WasteMode"/>, e cada modo tem o próprio campo de vagas: o entulho só vai às áreas de restos em folhas, o resto
    /// de cupim à mais perto entre as de restos em folhas e folhas em água, e a terra às de terra em areia e água. As folhas e a
    /// areia que nascem de um item ficam em células do mesmo modo. A água e a areia que saem das conversões são itens soltos
    /// (a água vai ao depósito de água ou aos canteiros; a areia ao despejo). Sem área do modo (ou cheia, ou sem caminho), o
    /// entulho e a terra seguem como terra (depósito de construção, canteiros ou despejo) e os restos ao despejo.
    /// </summary>
    public sealed partial class AntColony
    {
        const int WasteModeCount = 4;
        // Por modo: campo até as vagas livres das áreas daquele modo (criado no primeiro uso), vagas e quem leva para lá.
        readonly FlowField[] _wasteFields = new FlowField[WasteModeCount];
        readonly bool[] _wasteFieldIdle = { true, true, true, true };
        readonly int[] _wasteSpots = new int[WasteModeCount];
        readonly int[] _wasteRoomCells = new int[WasteModeCount];
        readonly int[] _wasteCarriers = new int[WasteModeCount];
        readonly List<int>[] _wasteSources = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        // Terra esperando virar areia e água nas áreas de terra (contada na rodada dos campos): cada uma precisa de uma vaga para a areia.
        int _wasteDirt;
        // Células com resíduo guardado e o tempo que falta para cada uma se decompor (refeito pela grade ao carregar).
        readonly List<int> _wasteCells = new List<int>();
        readonly List<float> _wasteTimes = new List<float>();
        int _trackedWasteKindVersion = -1;

        const int RemainsMode = (int)WasteMode.RemainsToLeaves, LeavesMode = (int)WasteMode.LeavesToWater, DirtMode = (int)WasteMode.DirtToWater,
            LeafNutrientMode = (int)WasteMode.LeavesToNutrient;

        /// <summary>Células das áreas de resíduos onde ainda cabe um resíduo (vazias, sem item, caminháveis), somando os modos.</summary>
        public int WasteSpots => _wasteSpots[RemainsMode] + _wasteSpots[LeavesMode] + _wasteSpots[DirtMode] + _wasteSpots[LeafNutrientMode];

        /// <summary>Vagas livres nas áreas do modo dado.</summary>
        public int WasteSpotsOf(WasteMode mode) => _wasteSpots[(int)mode];

        /// <summary>Resíduos se decompondo (inclui folhas e terra esperando a conversão).</summary>
        public int WasteCells => _wasteCells.Count;

        /// <summary>Modo da área de resíduos na célula (o padrão fora dela).</summary>
        public WasteMode WasteModeAt(int x, int y) => (WasteMode)_grid.GetWasteKind(x, y);

        /// <summary>Folhas que viraram água na área de resíduos desde o começo do mapa (não vai para o save).</summary>
        public int LeavesWatered { get; private set; }

        /// <summary>Folhas que viraram nutriente na sala de folhas desde o começo do mapa (não vai para o save).</summary>
        public int LeavesToNutrients { get; private set; }

        /// <summary>Terras que viraram areia e água na área de resíduos desde o começo do mapa (não vai para o save).</summary>
        public int DirtConverted { get; private set; }

        /// <summary>Resíduos (entulho, restos e terra) que as formigas depositaram nas áreas desde o começo do mapa (não vai para o save).</summary>
        public int WasteDeposited { get; private set; }

        readonly Dictionary<int, float> _keptTimes = new Dictionary<int, float>();

        // Até onde (em xadrez) o item que se converte procura células livres da mesma área para as folhas ou a areia.
        const int RemainsLeafReach = 12;
        // Sem lugar para todas as folhas, o resto espera este tempo e tenta de novo (o espaço abre conforme o resíduo some).
        const float RemainsRetrySeconds = 1f;
        readonly List<int> _leafSpots = new List<int>();

        /// <summary>
        /// Decompõe os resíduos quando o tempo de cada um acaba: o entulho some; o resto de cupim vira folhas guardadas na
        /// área (<see cref="TryRotRemains"/>); a folha (área de folhas em água) vira água; a terra (área de terra) vira água e areia.
        /// </summary>
        void TickWaste(float deltaTime)
        {
            SyncWasteKinds();
            int w = _grid.Width;
            for (int i = _wasteCells.Count - 1; i >= 0; i--)
            {
                _wasteTimes[i] -= deltaTime;
                if (_wasteTimes[i] > 0f)
                    continue;
                int cell = _wasteCells[i];
                int x = cell % w, y = cell / w;
                byte item = _grid.GetStored(x, y);
                CellMaterial material = StoredItems.MaterialOf(item);
                bool retry = false;
                if (material == CellMaterial.TermiteRemains)
                    retry = !TryRotRemains(x, y);
                else if (material == CellMaterial.Refuse)
                    _grid.SetStored(x, y, StoredItems.None);
                else if (IsConversionInput(x, y, item))
                {
                    // Folha → água; terra → água na célula e areia numa vaga perto (sem vaga, espera).
                    if (material == CellMaterial.PlantLeaf && _grid.GetWasteKind(x, y) == LeafNutrientMode)
                        retry = !TryMakeLeafNutrient(x, y, item);
                    else if (material == CellMaterial.PlantLeaf)
                    {
                        _grid.SetStored(x, y, StoredItems.None);
                        _grid.SetStored(x, y, StoredItems.Water);
                        LeavesWatered++;
                    }
                    else
                        retry = !TryConvertDirt(x, y);
                }
                // Outro item (a folha ou a terra saiu, a área mudou de modo): só deixa de acompanhar a célula.
                if (retry)
                {
                    _wasteTimes[i] = RemainsRetrySeconds;
                    continue;
                }
                int last = _wasteCells.Count - 1;
                _wasteCells[i] = _wasteCells[last];
                _wasteTimes[i] = _wasteTimes[last];
                _wasteCells.RemoveAt(last);
                _wasteTimes.RemoveAt(last);
            }
        }

        /// <summary>
        /// A área foi pintada, apagada ou mudou de modo: refaz a lista dos itens que se decompõem ou se convertem. Quem já
        /// estava na lista mantém o tempo que faltava; a folha ou a terra que passou a ficar numa área do modo dela começa com
        /// o tempo cheio, e a que deixou de estar vira item solto (as formigas a levam).
        /// </summary>
        void SyncWasteKinds()
        {
            if (_trackedWasteKindVersion == _grid.WasteKindVersion)
                return;
            _trackedWasteKindVersion = _grid.WasteKindVersion;
            _keptTimes.Clear();
            for (int i = 0; i < _wasteCells.Count; i++)
                _keptTimes[_wasteCells[i]] = _wasteTimes[i];
            _wasteCells.Clear();
            _wasteTimes.Clear();
            int w = _grid.Width;
            // Os que já eram acompanhados (inclusive resíduo fora da área apagada, que termina de se decompor).
            foreach (KeyValuePair<int, float> kept in _keptTimes)
            {
                int x = kept.Key % w, y = kept.Key / w;
                if (IsTrackedWaste(x, y))
                {
                    _wasteCells.Add(kept.Key);
                    _wasteTimes.Add(kept.Value);
                }
            }
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkWasteRoomCount(cx, cy) == 0 || _grid.GetChunkStoredCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (_grid.IsWasteRoom(x, y) && !_keptTimes.ContainsKey(y * w + x) && IsTrackedWaste(x, y))
                        TrackWaste(x, y);
                }
            }
            _keptTimes.Clear();
            // Itens que passam de soltos a convertendo (e o contrário) mudam os campos.
            _grid.TouchStore();
        }

        /// <summary>Resíduo (entulho, resto) em qualquer lugar, ou folha/terra que a área da célula converte.</summary>
        bool IsTrackedWaste(int x, int y)
        {
            byte item = _grid.GetStored(x, y);
            return StoredItems.KindOf(item) == StorageKind.Waste || IsConversionInput(x, y, item);
        }

        /// <summary>
        /// Item que a área da célula transforma onde está: folha numa área de folhas em água ou terra numa área de terra em
        /// areia e água. Não é item solto: ninguém o leva embora.
        /// </summary>
        bool IsConversionInput(int x, int y, byte item)
        {
            if (!_grid.IsWasteRoom(x, y))
                return false;
            int kind = _grid.GetWasteKind(x, y);
            return kind == LeavesMode || kind == LeafNutrientMode ? item == StoredItems.Of(CellMaterial.PlantLeaf)
                : kind == DirtMode && item == StoredItems.Of(CellMaterial.Spoil);
        }

        /// <summary>
        /// A folha da sala de folhas vira um bloco de nutriente na própria célula (1 folha, 1 bloco; pedido do desenvolvedor em
        /// 2026-10-24): a conversão não credita nada, a formiga leva o bloco até a rainha e ela o come, como os que brotam dos
        /// canteiros (<see cref="TryStartFetch"/>). Sem poder pôr o bloco, a folha fica e tenta de novo.
        /// </summary>
        bool TryMakeLeafNutrient(int x, int y, byte leaf)
        {
            byte shade = _grid.GetStoredShade(x, y);
            _grid.SetStored(x, y, StoredItems.None);
            if (_grid.SetNutrient(x, y, true))
            {
                LeavesToNutrients++;
                return true;
            }
            _grid.SetStored(x, y, leaf, shade);
            return false;
        }

        /// <summary>Tempo que o item recém-chegado à área leva para se decompor ou se converter.</summary>
        float WasteSeconds(int x, int y, byte item)
        {
            CellMaterial material = StoredItems.MaterialOf(item);
            if (material == CellMaterial.Spoil)
                return RaidSettings.DirtWaterSeconds * Effects.DirtWaterTime;
            if (material == CellMaterial.PlantLeaf)
                return _grid.GetWasteKind(x, y) == LeafNutrientMode
                    ? _settings.LeafNutrientSeconds
                    : RaidSettings.LeafWaterSeconds * Effects.WasteTime;
            return RaidSettings.WasteDecaySeconds * Effects.WasteTime;
        }

        void TrackWaste(int x, int y)
        {
            _wasteCells.Add(_grid.ToIndex(x, y));
            _wasteTimes.Add(WasteSeconds(x, y, _grid.GetStored(x, y)));
        }

        /// <summary>
        /// A terra em (<paramref name="x"/>, <paramref name="y"/>) vira um bloco de água ali e um de areia na vaga livre mais
        /// perto da mesma área (os dois itens soltos). Sem vaga, nada muda e devolve false.
        /// </summary>
        bool TryConvertDirt(int x, int y)
        {
            if (!FindLeafSpots(x, y, 1))
                return false;
            byte shade = _grid.GetStoredShade(x, y);
            int w = _grid.Width;
            int sand = _leafSpots[0];
            _grid.SetStored(x, y, StoredItems.None);
            _grid.SetStored(x, y, StoredItems.Water);
            _grid.SetStored(sand % w, sand / w, StoredItems.Of(CellMaterial.Sand), shade);
            DirtConverted++;
            return true;
        }

        /// <summary>
        /// Junta em <see cref="_leafSpots"/> até <paramref name="count"/> células livres em volta, do mesmo modo de área que
        /// (<paramref name="x"/>, <paramref name="y"/>), da mais perto para a mais longe.
        /// </summary>
        bool FindLeafSpots(int x, int y, int count)
        {
            _leafSpots.Clear();
            byte kind = _grid.GetWasteKind(x, y);
            for (int r = 1; r <= RemainsLeafReach && _leafSpots.Count < count; r++)
            for (int dy = r; dy >= -r && _leafSpots.Count < count; dy--)
            for (int dx = -r; dx <= r && _leafSpots.Count < count; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
                    continue;
                int lx = x + dx, ly = y + dy;
                if (_grid.InBounds(lx, ly) && IsLeafSpot(lx, ly) && _grid.GetWasteKind(lx, ly) == kind)
                    _leafSpots.Add(_grid.ToIndex(lx, ly));
            }
            return _leafSpots.Count >= count;
        }

        /// <summary>
        /// O resto de cupim em (<paramref name="x"/>, <paramref name="y"/>) vira <see cref="RaidSettings.RemainsLeaves"/> folhas
        /// guardadas: a própria célula e as livres mais próximas da mesma área. Na área de restos em folhas cada folha é um
        /// item de planta solto, que as formigas levam ao depósito ou aos canteiros; na de folhas em água, ela fica e vira
        /// água. Sem lugar para todas, nada muda e devolve false (tenta de novo depois).
        /// </summary>
        bool TryRotRemains(int x, int y)
        {
            int leaves = RaidSettings.RemainsLeaves;
            if (!FindLeafSpots(x, y, leaves - 1) && leaves > 0)
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
                if (IsConversionInput(lx, ly, StoredItems.Of(CellMaterial.PlantLeaf)))
                    TrackWaste(lx, ly);
            }
            RemainsRotted++;
            return true;
        }

        /// <summary>Célula livre da área de resíduos onde uma folha ou a areia pode ficar (sem material, item, nutriente nem água).</summary>
        bool IsLeafSpot(int x, int y) =>
            _grid.IsWasteRoom(x, y) && _grid.Get(x, y) == CellMaterial.Empty && _grid.GetStored(x, y) == StoredItems.None &&
            !_grid.HasWater(x, y) && !_grid.HasNutrient(x, y);

        /// <summary>Restos de cupim que já viraram folhas na área de resíduos desde o começo do mapa (não vai para o save).</summary>
        public int RemainsRotted { get; private set; }

        /// <summary>
        /// Volta a decompor os resíduos que vieram do save (o tempo recomeça cheio), inclusive as folhas e a terra que as
        /// áreas convertem. Saves de antes de 2026-10-19 guardavam o resíduo como material (célula sólida): ele vira item
        /// guardado na mesma célula.
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
                if (IsTrackedWaste(x, y))
                    TrackWaste(x, y);
            }
            _trackedWasteKindVersion = _grid.WasteKindVersion;
        }

        /// <summary>Campos até os pontos livres das áreas de resíduos, um por modo (só existe com área daquele modo pintada).</summary>
        void BuildWasteField()
        {
            _wasteDirt = 0;
            for (int m = 0; m < WasteModeCount; m++)
            {
                _wasteSpots[m] = 0;
                _wasteRoomCells[m] = 0;
                _wasteSources[m].Clear();
            }
            if (_grid.WasteRoomCount > 0)
            {
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
                        if (!_grid.IsWasteRoom(x, y))
                            continue;
                        int kind = Mathf.Min(_grid.GetWasteKind(x, y), WasteModeCount - 1);
                        _wasteRoomCells[kind]++;
                        if (IsWasteSpot(x, y))
                            _wasteSources[kind].Add(_grid.ToIndex(x, y));
                        else if (kind == DirtMode && _grid.GetStored(x, y) == StoredItems.Of(CellMaterial.Spoil))
                            _wasteDirt++;
                    }
                }
            }
            for (int m = 0; m < WasteModeCount; m++)
            {
                _wasteSpots[m] = _wasteSources[m].Count;
                if (_wasteRoomCells[m] == 0)
                {
                    if (!_wasteFieldIdle[m])
                    {
                        _wasteFields[m].Invalidate();
                        _wasteFieldIdle[m] = true;
                    }
                    continue;
                }
                _wasteFieldIdle[m] = false;
                _wasteFields[m] ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
                UpdateField(_wasteFields[m], _wasteSources[m]);
                _wasteSources[m].Clear();
            }
        }

        /// <summary>
        /// Cabe um resíduo na célula: na área de resíduos, livre (sem material, item, nutriente nem água) e caminhável. O
        /// resíduo guardado não bloqueia a passagem, então qualquer célula vazia da área serve (não só o chão).
        /// </summary>
        bool IsWasteSpot(int x, int y) =>
            _grid.IsWasteRoom(x, y) && !_grid.IsSolid(x, y) && _grid.GetStored(x, y) == StoredItems.None &&
            !_grid.HasNutrient(x, y) && !_grid.HasWater(x, y) && _walkability.IsWalkable(x, y);

        /// <summary>A área do modo recebe o material: entulho nas de restos em folhas; resto de cupim nas de restos e de folhas; terra nas de terra.</summary>
        bool WasteAccepts(int mode, CellMaterial material)
        {
            switch (mode)
            {
                case RemainsMode: return CellMaterials.IsWaste(material);
                case LeavesMode: return material == CellMaterial.TermiteRemains;
                case LeafNutrientMode: return CellMaterials.IsPlant(material);
                default: return IsBlockItem(material);
            }
        }

        /// <summary>Distância pelo campo das vagas do modo (inalcançável sem área, sem vaga ou sem caminho).</summary>
        int WasteDistance(int mode, int x, int y) =>
            _wasteSpots[mode] > 0 && !_wasteFieldIdle[mode] ? _wasteFields[mode].Get(x, y) : FlowField.Unreachable;

        /// <summary>
        /// Entulho ou restos na mão (ou, com <paramref name="dirt"/>, terra) e área do modo que os recebe com lugar e caminho:
        /// leva para lá. O resto de cupim vai à mais perto entre as áreas de restos em folhas e de folhas em água.
        /// </summary>
        bool TryStartWaste(Ant ant, bool dirt = false)
        {
            if (CellMaterials.IsPlant(ant.Carried))
                return false; // planta vai à sala de folhas por TryStartLeafRoom
            // Sala de resíduos parada ou no limite: segue o caminho de quando não há área (despejo).
            if (!SectionAllows(WorkSection.WasteRoom, ant))
                return false;
            int mode = -1;
            if (dirt)
            {
                if (IsBlockItem(ant.Carried) && DirtRoomLeft && WasteDistance(DirtMode, ant.X, ant.Y) != FlowField.Unreachable)
                    mode = DirtMode;
            }
            else if (CellMaterials.IsWaste(ant.Carried))
            {
                int best = FlowField.Unreachable;
                for (int m = RemainsMode; m <= LeavesMode; m++)
                {
                    if (!WasteAccepts(m, ant.Carried))
                        continue;
                    int d = WasteDistance(m, ant.X, ant.Y);
                    if (d != FlowField.Unreachable && (mode < 0 || d < best))
                    {
                        mode = m;
                        best = d;
                    }
                }
            }
            if (mode < 0)
                return false;
            ReleaseDumpSide(ant);
            ant.State = AntState.CarryingWaste;
            ant.WasteTarget = (byte)mode;
            _wasteCarriers[mode]++;
            return true;
        }

        /// <summary>
        /// Ainda cabe terra nas áreas de terra: cada terra guardada precisa de uma vaga livre para a areia ao se converter,
        /// então a terra (guardada e a caminho) fica abaixo das vagas livres. Sem isso a área enche de terra sem lugar para a
        /// areia e trava.
        /// </summary>
        /// <summary>
        /// Vagas livres nas salas de folhas que a colheita pode encher (0 com a seção de resíduos parada). Entram em
        /// <see cref="PlantSupplyDemand"/>: com a sala pintada, as formigas cortam planta mesmo sem canteiros nem depósito.
        /// </summary>
        int LeafRoomSpots => Work.IsEnabled(WorkSection.WasteRoom) && _wasteSpots[LeafNutrientMode] > 0 ? _wasteSpots[LeafNutrientMode] : 0;

        /// <summary>Há sala de folhas com vaga e caminho a partir da célula.</summary>
        bool LeafRoomReachable(int x, int y) =>
            LeafRoomSpots > 0 && WasteDistance(LeafNutrientMode, x, y) != FlowField.Unreachable;

        /// <summary>
        /// A formiga com planta na mão (que nenhum canteiro pede) leva para a sala de folhas, se houver vaga e caminho. Planta
        /// na mão vai na ordem: canteiros, depósito de alimentos, sala de folhas, chão.
        /// </summary>
        bool TryStartLeafRoom(Ant ant)
        {
            if (!CellMaterials.IsPlant(ant.Carried) || !SectionAllows(WorkSection.WasteRoom, ant) ||
                _wasteSpots[LeafNutrientMode] - _wasteCarriers[LeafNutrientMode] <= 0 ||
                WasteDistance(LeafNutrientMode, ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ReleaseDumpSide(ant);
            ant.State = AntState.CarryingWaste;
            ant.WasteTarget = LeafNutrientMode;
            _wasteCarriers[LeafNutrientMode]++;
            return true;
        }

        bool DirtRoomLeft => _wasteDirt + _wasteCarriers[DirtMode] < _wasteSpots[DirtMode];

        // Formigas indo buscar terra na pilha de fora para a área (recontadas a cada Tick).
        int _wasteDirtFetchers;

        /// <summary>
        /// Área de terra com lugar: a terra guardada, a que está a caminho e a que as formigas foram buscar ficam abaixo das
        /// vagas livres (cada uma precisa de uma vaga a mais para a areia, ver <see cref="DirtRoomLeft"/>).
        /// </summary>
        bool WasteRestockOpen => _wasteDirt + _wasteCarriers[DirtMode] + _wasteDirtFetchers < _wasteSpots[DirtMode];

        /// <summary>Uma área de terra quer terra da pilha de fora (o campo da pilha é montado por ela).</summary>
        bool WastePileWanted => _wasteDirt < _wasteSpots[DirtMode];

        // Valor de WastePileWanted na última montagem do campo da pilha.
        bool _builtWastePileWanted;

        /// <summary>
        /// Área de terra em areia e água (correção de 2026-10-21: antes só a terra cavada que iria ao despejo chegava à
        /// área, e com o depósito de construção aceitando nada chegava): a ociosa busca terra solta da pilha de fora e a leva
        /// para lá, como a reposição do depósito de construção, que vem antes na ordem e só chama formigas para as vagas dele.
        /// </summary>
        bool TryStartWasteRestock(Ant ant)
        {
            if (!WasteRestockOpen || !SectionAllows(WorkSection.WasteRoom, ant) || _builtSoilPileVersion < 0 ||
                WasteDistance(DirtMode, ant.X, ant.Y) == FlowField.Unreachable ||
                _soilPileField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.DewJob = false;
            ant.StoreTarget = StorageKind.None;
            ant.WasteDirtJob = true;
            ant.State = AntState.FetchingSoil;
            _wasteDirtFetchers++;
            UpdateFetchingSoil(ant);
            return true;
        }

        void CountWasteDirtFetcher(Ant ant)
        {
            if (ant.State != AntState.FetchingSoil && ant.State != AntState.GatheringSoil)
                ant.WasteDirtJob = false;
            else if (ant.WasteDirtJob)
                _wasteDirtFetchers++;
        }

        void CountWasteCarrier(Ant ant)
        {
            if (ant.State == AntState.CarryingWaste)
                _wasteCarriers[Mathf.Min(ant.WasteTarget, WasteModeCount - 1)]++;
        }

        void ResetWasteCounts()
        {
            System.Array.Clear(_wasteCarriers, 0, WasteModeCount);
            _wasteDirtFetchers = 0;
        }

        /// <summary>Desce o campo das vagas da área escolhida e guarda o resíduo na própria célula ou numa vizinha. Sem caminho, entrega como terra.</summary>
        void UpdateCarryingWaste(Ant ant)
        {
            int mode = Mathf.Min(ant.WasteTarget, WasteModeCount - 1);
            // A área mudou de modo ou sumiu no caminho: tenta outra que receba a carga; senão, a entrega normal.
            if (!WasteAccepts(mode, ant.Carried))
            {
                StartDelivery(ant);
                return;
            }
            int d = WasteDistance(mode, ant.X, ant.Y);
            if (d == FlowField.Unreachable)
            {
                DeliverWithoutWasteRoom(ant);
                return;
            }
            if (d <= 1 && TryPlaceWaste(ant, mode))
                return;
            if (d > 0 && StepDownhill(ant, _wasteFields[mode]))
                return;
            // No ponto, mas outra formiga já o ocupou: espera o campo ser refeito.
            Hold(ant);
        }

        void DeliverWithoutWasteRoom(Ant ant)
        {
            if (CellMaterials.IsPlant(ant.Carried))
            {
                // A sala de folhas ficou inalcançável: a planta volta ao caminho normal (UpdateStoring decide de novo).
                ant.State = AntState.Carrying;
                return;
            }
            ant.State = AntState.Carrying;
            ant.StoreTarget = StorageKind.None;
            if (BlockDepot ? IsBlockItem(ant.Carried) && TryStartStock(ant, StorageKind.Blocks) : TryStartBedSoil(ant))
                return;
            AssignDumpSide(ant);
        }

        bool TryPlaceWaste(Ant ant, int mode)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                if (!IsWasteSpot(x, y) || _grid.GetWasteKind(x, y) != mode)
                    continue;
                // O resto de cupim fica como resto (vira folhas ao se decompor); a terra fica como terra fofa (vira areia e
                // água); o entulho vira resíduo comum.
                CellMaterial deposited = CellMaterials.IsPlant(ant.Carried) ? CellMaterial.PlantLeaf
                    : ant.Carried == CellMaterial.TermiteRemains ? CellMaterial.TermiteRemains
                    : CellMaterials.IsWaste(ant.Carried) ? CellMaterial.Refuse : CellMaterial.Spoil;
                if (deposited == CellMaterial.PlantLeaf)
                    _plantsInFlight = Mathf.Max(0, _plantsInFlight - 1);
                _grid.SetStored(x, y, StoredItems.Of(deposited), ant.CarriedShade);
                Emit(SfxId.Drop, x, y);
                TrackWaste(x, y);
                if (deposited == CellMaterial.Spoil)
                    _wasteDirt++;
                WasteDeposited++;
                ant.DropTop();
                ant.Cooldown = _settings.DropSeconds;
                if (x != ant.X)
                    ant.FacingX = x > ant.X ? 1 : -1;
                // Sobrou carga: o próximo pixel do mesmo tipo vai para outro ponto da mesma área; senão, a entrega normal.
                if (ant.CarriedCount == 0)
                    ant.State = AntState.Idle;
                else if (!WasteAccepts(mode, ant.Carried))
                    StartDelivery(ant);
                return true;
            }
            return false;
        }
    }
}
