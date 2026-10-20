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
        // A última gota esperou porque a sala estava cheia (barra de progresso cinza).
        bool _dewBlocked;
        // Altura da coluna mais alta da sala (células pintadas) e colunas com ponto de queda (inclusive as já alagadas):
        // o teto de água da sala vem das duas, então a sala larga guarda mais.
        int _dewMaxHeight;
        int _dewDropColumns;
        int _dewSoilPlaced;
        int _dewLeavesPlaced;
        int _dewSoilSpots;
        int _dewLeafSpots;
        int _dewCarriers;
        bool _dewActive;
        // Pontos de terra fofa que uma formiga levando terra reservou (Ant.SeekTarget), recontados a cada Tick: saem do campo
        // da terra para as outras irem a outro ponto, como as reservas da escavação (pedido do desenvolvedor em 2026-10-19).
        readonly HashSet<int> _dewSoilClaims = new HashSet<int>();
        readonly List<int> _dewSoilFieldSources = new List<int>();
        long _dewClaimHash;
        long _builtDewClaimHash;
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

        /// <summary>
        /// Quantas formigas levando terra a sala chama: uma por ponto livre, como a escavação chama uma por pixel marcado. Cada
        /// uma reserva o ponto dela ao chegar com a terra (<see cref="UpdateBringingDewSoil"/>).
        /// </summary>
        int WantedDewCarriers => _dewSoilSpots;

        /// <summary>As reservas de ponto de terra mudaram desde a última rodada: o campo da terra precisa tirar ou devolver pontos.</summary>
        bool DewClaimsChanged => _dewClaimHash != _builtDewClaimHash;

        // ---------------------------------------------------------------- etapas

        /// <summary>
        /// Refaz os pontos da sala (terra fofa, folha, queda da gota) e o campo até a terra. Os pontos de folha entram em
        /// <paramref name="storeSources"/> (as células ao lado deles): a colheita e a entrega da planta usam o mesmo campo dos canteiros.
        /// </summary>
        void BuildDewSpots(List<int> storeSources)
        {
            _dewDirty = false;
            _builtDewClaimHash = _dewClaimHash;
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
            _dewMaxHeight = 0;
            _dewDropColumns = 0;
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
            // Ponto reservado continua em _dewSoilSet (a dona ainda o enche), mas sai do campo: as outras descem para outro.
            _dewSoilFieldSources.Clear();
            foreach (int i in _dewSoilCells)
            {
                if (!_dewSoilClaims.Contains(i))
                    _dewSoilFieldSources.Add(i);
            }
            UpdateField(_dewSoilField, _dewSoilFieldSources);
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
            _dewMaxHeight = Mathf.Max(_dewMaxHeight, height);
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
            if (!_grid.IsDewRoom(x, yDrop) || _grid.IsSolid(x, yDrop))
                return covered;
            _dewDropColumns++;
            if (!_grid.HasWater(x, yDrop))
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

        /// <summary>Quanto falta (0 a 1) para a próxima gota; -1 sem sala pronta. Para a barra de progresso.</summary>
        public float DewProgress => _dewActive && _dewDropCells.Count > 0 ? Mathf.Clamp01(_dewTimer / Mathf.Max(0.01f, _settings.DewDropSeconds)) : -1f;

        /// <summary>A sala esperava as formigas levarem a água (cheia) na última gota.</summary>
        public bool DewBlocked => _dewBlocked;

        /// <summary>Pontos de queda da sala (um por coluna): cada um tem a sua barra de progresso.</summary>
        public int DewDropCellCount => _dewDropCells.Count;

        public Vector2Int DewDropCell(int index)
        {
            int i = _dewDropCells[index];
            return new Vector2Int(i % _grid.Width, i / _grid.Width);
        }

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
            _dewTimer += deltaTime * (1f + covered * _settings.DewTendBoost * Effects.GardenerDewRate) / Effects.DewDropTime;
            if (_dewTimer < _settings.DewDropSeconds)
                return;
            // Cheia ou sem ponto livre, a gota espera o próximo intervalo.
            _dewTimer = 0f;
            // Teto por coluna com ponto de queda (pedido do desenvolvedor em 2026-10-20: antes só a altura contava, e a sala larga
            // enchia em dois intervalos e parava); acima dele, só as células livres da sala limitam a gota.
            int room = _settings.DewWaterPerHeight * _dewMaxHeight * _dewDropColumns + Effects.DewRoomWaterBonus -
                       (_dewDropCells.Count == 0 ? int.MaxValue : CountDewWater());
            _dewBlocked = room <= 0;
            if (_dewBlocked)
                return;
            // Cada ponto de queda solta a sua gota a cada intervalo (o tamanho da sala conta, como os canteiros da sala de
            // nutrientes); a partir de uma posição aleatória, para a trava da sala cheia não favorecer sempre o mesmo lado.
            // G1 (Folhas coletoras): cada ponto rende o dobro (a segunda gota cai na célula livre logo abaixo).
            int perSlot = Mathf.Max(1, Mathf.RoundToInt(Effects.DewYield));
            int start = _rng.Next(_dewDropCells.Count);
            for (int k = 0; k < _dewDropCells.Count && room > 0; k++)
            {
                int i = _dewDropCells[(start + k) % _dewDropCells.Count];
                int x = i % _grid.Width, y = i / _grid.Width;
                if (!_grid.IsSolid(x, y + 1))
                    continue;
                for (int d = 0, yy = y; d < perSlot && room > 0 && _grid.IsDewRoom(x, yy) && !_grid.IsSolid(x, yy); yy--)
                {
                    if (_grid.HasWater(x, yy))
                        continue;
                    _grid.SetWater(x, yy, true);
                    DewDrops++;
                    room--;
                    d++;
                }
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
            if (!SectionAllows(WorkSection.Dew, ant) || !_dewActive || _dewDropCells.Count == 0 || _dewTendCount >= DewBlockCount ||
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

        /// <summary>
        /// A sala de nutrientes quer esta jardineira e a seção Jardim tem vaga para ela. Sem a vaga, sair do orvalho só a
        /// deixava parada: o cuidado da sala de nutrientes a recusava e ela voltava ao orvalho no passo seguinte (relato de
        /// 2026-10-19, seção Jardim cheia no painel).
        /// </summary>
        bool GardenCanTake(Ant ant) => WantsTender(ant, _garden) && SectionAllows(WorkSection.Garden, ant);

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
                _dewTendField.Get(ant.X, ant.Y) == FlowField.Unreachable || GardenCanTake(ant))
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

        /// <summary>
        /// Água da sala que as formigas alcançam. A de bolsão fechado (sem caminho de casa) não conta: ninguém a leva, e o jogador
        /// nem a vê, então ela travava a sala "cheia" com menos água à mostra que o teto (relato de 2026-10-20).
        /// </summary>
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
                    if (_grid.IsDewRoom(x, y) && _grid.HasWater(x, y) && IsReachableFromHome(x, y))
                        count++;
                }
            }
            return count;
        }

        // ---------------------------------------------------------------- terra fofa e folha

        /// <summary>Trava o ponto de terra fofa para as outras formigas; o campo da terra o tira na próxima rodada.</summary>
        void ClaimDewSoil(int index)
        {
            if (_dewSoilClaims.Add(index))
                _dewClaimHash += index * 2654435761L;
        }

        /// <summary>O ponto ainda pede terra fofa e nenhuma outra formiga o reservou.</summary>
        bool IsFreeDewSoilSpot(Ant ant, int index)
        {
            int x = index % _grid.Width, y = index / _grid.Width;
            return _dewSoilSet.Contains(index) && (index == ant.SeekTarget || !_dewSoilClaims.Contains(index)) && IsDewSoilSpot(x, y);
        }

        /// <summary>
        /// Levando terra fofa para o teto da sala Gotas de Orvalho, com reserva (mesma ideia da escavação, <see cref="SeekWork"/>):
        /// traça o caminho pelo campo da terra até um ponto livre, reserva o ponto (<see cref="Ant.SeekTarget"/>) e segue o caminho
        /// guardado; encostada nele, põe a terra. O ponto reservado sai do campo na próxima rodada, então as outras vão a outro
        /// em vez de fazerem fila no mesmo (relato de 2026-10-19: a sala era coberta de uma em uma formiga). Sem ponto livre
        /// alcançável, a terra vai ao despejo.
        /// </summary>
        void UpdateBringingDewSoil(Ant ant)
        {
            if (_dewSoilSpots == 0 || _dewSoilField == null)
            {
                ant.ClearSeek();
                SendSoilToDump(ant);
                return;
            }
            if (ant.SeekTarget >= 0 && !IsFreeDewSoilSpot(ant, ant.SeekTarget))
                ant.ClearSeek();
            if (ant.SeekTarget < 0 && !PlanDewSoil(ant))
                return;

            int w = _grid.Width;
            int tx = ant.SeekTarget % w, ty = ant.SeekTarget / w;
            if (tx == ant.X && ty == ant.Y)
            {
                // Em cima do ponto (a terra não cai na própria formiga): sai para o lado e larga dali.
                Wander(ant);
                return;
            }
            if (Mathf.Abs(tx - ant.X) <= 1 && Mathf.Abs(ty - ant.Y) <= 1)
            {
                if (!AntAt(tx, ty))
                    PlaceDewSoil(ant, tx, ty);
                else
                    Hold(ant); // outra formiga passando por cima do ponto: espera ela sair
                return;
            }
            if (StepPath(ant) > 0)
                return;
            // Caminho cortado (caiu, a célula fechou) ou terminou longe do ponto: traça de novo no próximo passo.
            ant.ClearSeek();
            Hold(ant);
        }

        /// <summary>
        /// Traça o caminho até o ponto livre mais perto pelo campo da terra e o reserva. Se o ponto do fim já foi reservado por
        /// outra neste passo (o campo ainda não foi refeito), pega um ponto livre vizinho; sem nenhum, espera a próxima rodada.
        /// <c>false</c> = a formiga já agiu (despejo ou espera).
        /// </summary>
        bool PlanDewSoil(Ant ant)
        {
            if (_dewSoilField.Get(ant.X, ant.Y) == FlowField.Unreachable)
            {
                SendSoilToDump(ant);
                return false;
            }
            if (!TracePath(ant, _dewSoilField, out int x, out int y, out bool tooFar))
            {
                if (!(tooFar && StepDownhill(ant, _dewSoilField)))
                    Hold(ant);
                return false;
            }
            int target = _grid.ToIndex(x, y);
            if (!IsFreeDewSoilSpot(ant, target))
            {
                target = -1;
                int seen = 0;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                    if (_grid.InBounds(nx, ny) && IsFreeDewSoilSpot(ant, _grid.ToIndex(nx, ny)) && _rng.Next(++seen) == 0)
                        target = _grid.ToIndex(nx, ny);
                }
                if (target < 0)
                {
                    ant.ClearSeek();
                    Hold(ant);
                    return false;
                }
            }
            ant.SeekTarget = target;
            ant.SeekRock = false;
            ClaimDewSoil(target);
            return true;
        }

        /// <summary>Põe a terra fofa que leva no ponto (<paramref name="x"/>, <paramref name="y"/>); sobrando terra, entrega o resto.</summary>
        void PlaceDewSoil(Ant ant, int x, int y)
        {
            _grid.Set(x, y, CellMaterial.Spoil, ant.CarriedShade);
            Emit(SfxId.Drop, x, y);
            _dewSoilSet.Remove(_grid.ToIndex(x, y));
            _dewSoilSpots = Mathf.Max(0, _dewSoilSpots - 1);
            _dewDirty = true;
            ant.ClearSeek();
            ant.DropTop();
            ant.DewJob = false;
            ant.Cooldown = _settings.DropSeconds;
            if (x != ant.X)
                ant.FacingX = x > ant.X ? 1 : -1;
            if (ant.CarriedCount > 0)
                StartDelivery(ant);
            else
                ant.State = AntState.Idle;
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
                if (ant.FromStore)
                    _plantsToBeds = Mathf.Max(0, _plantsToBeds - 1);
                else
                    _plantsInFlight = Mathf.Max(0, _plantsInFlight - 1);
                HarvestedPixels++;
                Emit(SfxId.Plant, x, y);
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
