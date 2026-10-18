using System;
using System.Collections.Generic;
using Fabula.Incrementant.Diagnostics;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Formigas, navegação e economia da colônia. Ciclo: ociosa → vai até um pixel marcado → cava →
    /// carrega até uma faixa de despejo na superfície → solta o pixel (vira material granular) → ociosa.
    /// Cada abertura dos túneis na superfície (a entrada e as que o jogador cavar) ganha uma faixa de despejo de cada lado.
    /// A formiga leva o pixel para a abertura mais perto e, nela, para o lado com menos terra, para o monte crescer
    /// equilibrado.
    /// Modo construir (marcas de construção): a formiga pega um pixel da terra despejada fora do formigueiro, leva até
    /// uma célula marcada e a preenche. Vai do ponto mais fundo para o mais perto da saída (o inverso de cavar), então
    /// nunca fecha a própria saída.
    /// Rainha (<see cref="Queen"/>): o dobro do tamanho no desenho; só anda dentro da sala da rainha pintada pelo
    /// jogador (a maior, se houver várias). As formigas ociosas vagam por toda a região do formigueiro ligada a ela.
    /// Nutrientes: quem corta (<see cref="AntRoles.Harvests"/>: cortadeira, e comum/carregadora sem outra tarefa; o campo e o
    /// alvo "garden" têm esse nome por história, a jardineira não corta) escolhem sozinhas a planta-alvo (a maior que já
    /// cresceu o mínimo e, no empate, a mais perto da entrada), cortam-na de cima para baixo e levam cada pixel para um slot livre
    /// da sala de nutrientes (sem slot livre, ninguém colhe); de lá, qualquer formiga leva cada nutriente à rainha, que o
    /// consome e soma em <see cref="Nutrients"/>, a moeda das formigas novas.
    /// </summary>
    public sealed partial class AntColony
    {
        readonly PixelGrid _grid;
        readonly ColonySettings _settings;
        readonly TerrainLayout _layout;
        readonly System.Random _rng;
        readonly List<Ant> _ants = new List<Ant>();
        readonly List<int> _sources = new List<int>();
        // Memória de trabalho que os campos dividem na correção incremental (um por campo custaria dezenas de MB).
        readonly FlowField.Scratch _fieldScratch;
        readonly FlowField _digField;
        readonly List<DumpSide> _dumpSides = new List<DumpSide>();
        readonly List<FlowField> _fieldPool = new List<FlowField>();
        // Aberturas como (x mínimo, x máximo), inclusivos: todas as encontradas e as que receberam faixa.
        readonly List<Vector2Int> _allOpenings = new List<Vector2Int>();
        readonly List<Vector2Int> _scannedOpenings = new List<Vector2Int>();
        readonly List<Vector2Int> _openings = new List<Vector2Int>();
        readonly List<Vector2Int> _dumpStrips = new List<Vector2Int>();
        RectInt _sidesArea;
        readonly FlowField _homeField;
        // Construção: distância até a saída (superfície), até a terra solta lá fora e até onde dá para encaixar um pixel.
        readonly FlowField _exitField;
        readonly FlowField _pileField;
        readonly FlowField _buildField;
        readonly List<int> _buildSources = new List<int>();
        readonly FlowField _siteField;
        readonly List<int> _siteSources = new List<int>();
        readonly List<int> _reserved = new List<int>();
        // Varredura limitada usada pela validação de passagem (carimbo evita limpar o array a cada uso).
        readonly int[] _floodStamp;
        readonly int[] _floodQueue;
        readonly int[] _ringX = new int[8];
        readonly int[] _ringY = new int[8];
        readonly int[] _ringRoot = new int[8];
        int _floodCounter;
        bool _hasBuildWork;
        bool _hasSite;
        int _activeBuilders;
        int _buildFieldsVersion = -1;
        readonly WalkabilityMap _walkability;
        readonly int[] _candidates = new int[8];

        // Rainha: a sala dela é o maior grupo conectado de células pintadas e caminháveis. Cada grupo achado ganha um
        // rótulo novo (nunca reaproveitado), então rótulos velhos nas células não precisam ser limpos.
        readonly int[] _roomLabel;
        readonly List<int> _roomCells = new List<int>();
        readonly FlowField _queenRoomField;
        // Região do formigueiro (só túnel, sem atravessar marcas de construção) ligada à rainha: onde as ociosas vagam.
        readonly FlowField _colonyField;
        // Criação: o ciclo de ovo/larva/pupa e o espaço que o limita (recontado quando o formigueiro muda).
        readonly BroodCycle _brood;
        readonly List<AntRole> _born = new List<AntRole>();
        int _usableCells;
        int _nurseryCells;
        readonly List<Vector2Int> _nurseryFloor = new List<Vector2Int>();
        readonly List<Vector2Int> _nurserySpots = new List<Vector2Int>();
        readonly HashSet<Vector2Int> _nurseryTaken = new HashSet<Vector2Int>();
        // Transporte do ovo: campo até os pontos do berçário menos ocupados (refeito quando a cria muda de lugar ou nasce).
        readonly FlowField _nurseryField;
        bool _nurseryDirty = true;
        int _nurseryMinLoad;
        int _labelCounter;
        int _queenLabel;
        int _homeAnchor = int.MinValue;
        /// <summary>Espera da rainha quando não há sala (ou ela não alcança a sala): fica parada e confere de novo.</summary>
        const float QueenWaitSeconds = 0.5f;

        // Nutrientes: campo até os slots livres da sala de nutrientes e campo até os nutrientes guardados.
        readonly FlowField _storeField;
        readonly FlowField _nutrientField;
        int _freeSlots;

        // Quem corta: campo até a linha de cima da planta-alvo e a planta-alvo.
        // O alvo é escolhido sozinho (ver PickGardenTarget) e mantido até não restar o que cortar.
        readonly FlowField _gardenField;
        // Rocha marcada, alcançável só pela escavadora com E2 (a terra tem o próprio campo, _digField).
        readonly FlowField _rockField;
        bool _builtRockOpen;
        VegetationSimulation _vegetation;
        int _harvesters;
        // Escavadoras vivas: a rocha só entra nos campos quando alguma pode quebrá-la (E2).
        int _diggers;
        // Pixels de planta (em pé ou soltos) que alguma formiga está cortando/pegando: travados para as outras irem a outro.
        readonly HashSet<int> _claimed = new HashSet<int>();
        long _claimHash;
        long _builtClaimHash;
        int _gardenTarget = -1;
        int _gardenRow, _gardenMinX, _gardenMaxX;
        bool _gardenDirty;
        float _gardenRepickTimer;
        /// <summary>Intervalo (s) entre tentativas de escolher uma planta quando não há alvo.</summary>
        const float GardenRepickSeconds = 1f;
        bool _nutrientsReachQueen;
        int _plantsInFlight;
        int _fetchers;
        int _builtNutrientVersion = -1;
        (bool, bool, bool) _builtHarvestOpen;

        int _builtStructureVersion = -1;
        int _builtMarkVersion = -1;
        float _fieldTimer;

        public IReadOnlyList<Ant> Ants => _ants;

        /// <summary>
        /// Quem roda vários passos num mesmo frame liga isto em todos menos no último: a rodada de campos de navegação (cara, e
        /// em tempo simulado) acontece no máximo uma vez por frame em vez de uma a cada 0,1 s simulado. Em x8, ou com o frame
        /// lento, isso evita que o custo dos campos cresça com o número de passos. A primeira rodada nunca é adiada.
        /// </summary>
        public bool DeferFieldRefresh { get; set; }

        /// <summary>Rodadas de campos já feitas (para testes e diagnóstico).</summary>
        public int FieldRounds { get; private set; }

        /// <summary>Tempo por etapa da simulação e da renderização, mostrado no painel de estatísticas (F11).</summary>
        public SimProfiler Profiler { get; } = new SimProfiler();

        /// <summary>A rainha. Não entra em <see cref="Ants"/>: não cava, não carrega e não se afoga.</summary>
        public Ant Queen { get; }

        /// <summary>Células da sala em que a rainha fica (a maior sala pintada; 0 = nenhuma sala utilizável).</summary>
        public int QueenRoomSize => _roomCells.Count;

        /// <summary>A rainha já está dentro da sala dela.</summary>
        public bool QueenInRoom => _queenLabel != 0 && _roomLabel[_grid.ToIndex(Queen.X, Queen.Y)] == _queenLabel;

        /// <summary>Há caminho da rainha até a sala dela (fora dela e sem caminho, ela espera onde está).</summary>
        public bool QueenCanReachRoom => _queenRoomField.Get(Queen.X, Queen.Y) != FlowField.Unreachable;

        /// <summary>A célula faz parte da sala em que a rainha fica (recalculada com os campos de navegação).</summary>
        public bool IsQueenArea(int x, int y) =>
            _queenLabel != 0 && _grid.InBounds(x, y) && _roomLabel[_grid.ToIndex(x, y)] == _queenLabel;
        /// <summary>Terra despejada, guardada como estatística (desde os nutrientes, não é mais a moeda).</summary>
        public int StoredPixels { get; private set; }
        public int TotalDelivered { get; private set; }

        /// <summary>Nutrientes consumidos pela rainha e ainda não gastos: a moeda das formigas novas.</summary>
        public int Nutrients { get; private set; }

        /// <summary>Nutrientes que a rainha consumiu desde o começo do mapa.</summary>
        public int TotalNutrients { get; private set; }

        /// <summary>Pixels de planta colhidos e guardados na sala de nutrientes desde o começo do mapa.</summary>
        public int HarvestedPixels { get; private set; }

        /// <summary>Segundos que um pixel de planta guardado leva para virar nutriente (ver <see cref="ColonySettings"/>).</summary>
        public float NutrientCureSeconds => _settings.NutrientCureSeconds;

        /// <summary>Slots livres na sala de nutrientes (células pintadas, caminháveis e sem nutriente).</summary>
        public int FreeNutrientSlots => _freeSlots;

        /// <summary>Há slot livre para mais um pixel de planta (contando os que já estão sendo colhidos ou levados).</summary>
        public bool CanHarvest => _freeSlots - _plantsInFlight - _grid.PlantDebrisCount > 0;

        /// <summary>
        /// Os cortadores pararam porque a sala de nutrientes encheu e voltaram ao formigueiro. Só voltam a cortar quando
        /// os nutrientes guardados caem abaixo da metade da capacidade (histerese, para não alternar a cada slot que abre).
        /// </summary>
        public bool HarvestPaused => _harvestPaused;
        bool _harvestPaused;

        /// <summary>Há slot para levar um pixel que já está solto no chão (ele mesmo já está contado em <c>PlantDebrisCount</c>).</summary>
        bool CanStoreDebris => _freeSlots - _plantsInFlight > 0;

        /// <summary>Formigas que morreram afogadas desde o começo do mapa.</summary>
        public int DrownedAnts { get; private set; }

        public TerrainLayout Layout => _layout;

        /// <summary>Faixas de despejo ativas (x mínimo e máximo, inclusivos). Mudam quando uma abertura surge ou fecha.</summary>
        public IReadOnlyList<Vector2Int> DumpStrips => _dumpStrips;

        /// <summary>Aberturas na superfície que recebem despejo (0 = nenhuma achada; valem a faixa configurada e o espelho).</summary>
        public int OpeningCount => _openings.Count;

        /// <summary>Tempo simulado acumulado (s). Referência para <see cref="Ant.MoveStart"/>.</summary>
        public float SimulationTime { get; private set; }

        /// <summary>Limite de formigas da colônia (<see cref="ColonySettings.MaxAnts"/>).</summary>
        public int MaxAnts => _settings.MaxAnts;

        /// <summary>O ciclo de criação: ovos, larvas e pupas reservados e o relógio da postura.</summary>
        public BroodCycle Brood => _brood;

        /// <summary>Vagas de adulta: área escavada e acessível do formigueiro dividida por <see cref="ColonySettings.CellsPerAdultSlot"/>, limitada por <see cref="ColonySettings.MaxAnts"/>.</summary>
        public int AdultCapacity => Mathf.Min(_settings.MaxAnts, _usableCells / Mathf.Max(1, _settings.CellsPerAdultSlot));

        /// <summary>Vagas de cria: células vazias e alcançáveis do berçário dividido por <see cref="ColonySettings.CellsPerNurserySlot"/>.</summary>
        public int NurserySlots => _nurseryCells / Mathf.Max(1, _settings.CellsPerNurserySlot);

        /// <summary>Largura (células) que cada cria ocupa no chão do berçário: a do sprite (18 px a 6 px por célula).</summary>
        public const int NurserySpotSpacing = 3;

        /// <summary>Altura (células) que cada cria ocupa no berçário: a do sprite (12 px a 6 px por célula).</summary>
        public const int NurserySpotHeight = 2;

        /// <summary>
        /// Células (chão do berçário alcançável) onde a cria é desenhada; <see cref="BroodRecord.Slot"/> indexa esta lista.
        /// Vazia sem berçário. Refeita quando o formigueiro muda.
        /// </summary>
        public IReadOnlyList<Vector2Int> NurserySpots => _nurserySpots;

        /// <summary>Reserva de nutrientes que a rainha preserva antes de pôr um ovo.</summary>
        public int MinNutrientReserve => _settings.MinNutrientReserve;

        /// <summary>Por que a rainha está (ou não) pondo ovos agora.</summary>
        public BreedingState Breeding => _brood.State;

        /// <summary>A árvore da rainha: classes liberadas e melhorias compradas (ver <see cref="TryBuy"/>).</summary>
        public QueenTree Tree { get; } = new QueenTree();

        public AntColony(PixelGrid grid, ColonySettings settings, TerrainLayout layout, int seed)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _layout = layout;
            _rng = new System.Random(seed);
            _fieldScratch = new FlowField.Scratch(grid.Width, grid.Height);
            _digField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _homeField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _exitField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _pileField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _buildField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _siteField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _floodStamp = new int[grid.Width * grid.Height];
            _floodQueue = new int[grid.Width * grid.Height];
            _walkability = new WalkabilityMap(grid);
            _chunkSeen = new int[grid.ChunkColumns * grid.ChunkRows];
            Array.Fill(_chunkSeen, -1);
            _roomLabel = new int[grid.Width * grid.Height];
            _queenRoomField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _colonyField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _brood = new BroodCycle(settings);
            _nextRoleProvider = NextRole;
            _costProvider = CostOf;
            _nurseryField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _storeField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _nutrientField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _gardenField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _rockField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _curingField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            // Sem sorteio aqui: a rainha não muda a sequência aleatória das formigas.
            Queen = new Ant { X = layout.Nest.x, Y = layout.Nest.y, FacingX = 1 };
            PushOutOfSolid(Queen);
            SnapVisual(Queen);
            // As faixas já existem desde o começo (os campos delas vêm no primeiro Tick).
            UpdateDumpSides();

            // As iniciais são todas comuns: as classes só existem depois de compradas na árvore da rainha.
            for (int i = 0; i < settings.InitialAnts; i++)
                Spawn(AntRole.Worker);
        }

        /// <summary>Nova formiga do papel dado, nascida onde a rainha está.</summary>
        public Ant Spawn(AntRole role)
        {
            var ant = new Ant { X = Queen.X, Y = Queen.Y, FacingX = _rng.Next(2) == 0 ? -1 : 1, Role = role };
            PushOutOfSolid(ant);
            SnapVisual(ant);
            _ants.Add(ant);
            return ant;
        }

        /// <summary>
        /// Papel da próxima formiga: o escolhido no modo específico; no automático, o que está mais abaixo da parte
        /// desejada (<see cref="AntRoles.TargetWeight"/>, entre os papéis que existem), com empate pela ordem de <see cref="AntRoles.All"/>.
        /// </summary>
        AntRole NextRole()
        {
            // Só os papéis liberados na árvore; um alvo ainda bloqueado (save antigo, escolha anterior) cai no automático.
            if (QueenMode == QueenMode.Specific && Tree.IsRoleUnlocked(QueenTarget))
                return QueenTarget;
            // As crias já reservadas contam como adultas do papel delas, senão o automático repetiria o mesmo papel.
            int total = _ants.Count + _brood.Count + 1;
            float totalWeight = 0f;
            foreach (AntRole role in AntRoles.All)
            {
                if (Tree.IsRoleUnlocked(role))
                    totalWeight += AntRoles.TargetWeight(role);
            }
            AntRole best = AntRole.Worker;
            float bestDeficit = float.NegativeInfinity;
            foreach (AntRole role in AntRoles.All)
            {
                if (!Tree.IsRoleUnlocked(role))
                    continue;
                float deficit = AntRoles.TargetWeight(role) / totalWeight * total - CountOf(role) - _brood.CountOf(role);
                if (deficit > bestDeficit + 0.0001f)
                {
                    best = role;
                    bestDeficit = deficit;
                }
            }
            return best;
        }

        /// <summary>
        /// Compra um nó da árvore da rainha com os nutrientes: sem saldo, sem pré-requisito, já comprado ou ainda sem
        /// sistema no jogo, nada é debitado nem aplicado (o motivo vai no retorno).
        /// </summary>
        public BuyBlock TryBuy(QueenNode node)
        {
            BuyBlock block = Tree.Check(node);
            if (block != BuyBlock.None)
                return block;
            int cost = QueenTree.Def(node).Cost;
            if (Nutrients < cost)
                return BuyBlock.InsufficientNutrients;
            Tree.Grant(node);
            Nutrients -= cost;
            return BuyBlock.None;
        }

        /// <summary>Custo (nutrientes) de criar uma formiga do papel dado: o alimento por cria do design, estável (não cresce com a colônia).</summary>
        public int CostOf(AntRole role) => AntRoles.FoodCost(role);

        /// <summary>Quantas formigas vivas têm o papel dado.</summary>
        public int CountOf(AntRole role)
        {
            int count = 0;
            foreach (Ant a in _ants)
                count += a.Role == role ? 1 : 0;
            return count;
        }

        /// <summary>Como a rainha escolhe o papel das próximas formigas (política da rainha).</summary>
        public QueenMode QueenMode { get; private set; }

        /// <summary>Papel criado no modo <see cref="QueenMode.Specific"/>.</summary>
        public AntRole QueenTarget { get; private set; }

        /// <summary>Papel que a próxima formiga terá, ou <c>null</c> com a rainha pausada.</summary>
        public AntRole? NextAntRole => QueenMode == QueenMode.Paused ? (AntRole?)null : NextRole();

        /// <summary>Define a política da rainha. Não muda as formigas que já existem.</summary>
        public void SetQueenPolicy(QueenMode mode, AntRole target)
        {
            QueenMode = mode;
            QueenTarget = target;
        }

        /// <summary>Liga a vegetação de onde as jardineiras escolhem as plantas. Sem ela, ninguém colhe.</summary>
        public void AttachVegetation(VegetationSimulation vegetation)
        {
            _vegetation = vegetation;
            _gardenTarget = -1;
            _gardenDirty = true;
        }

        /// <summary>Planta (coluna da raiz) que as jardineiras estão colhendo; -1 = nenhuma.</summary>
        public int GardenTarget => _gardenTarget;

        /// <summary>
        /// Avança o ciclo de criação: debita o alimento de um ovo novo, desenvolve as crias e faz nascer as adultas prontas
        /// (onde a rainha está). Pausada, a rainha não põe ovos novos, mas as crias existentes seguem.
        /// </summary>
        Func<AntRole> _nextRoleProvider;
        Func<AntRole, int> _costProvider;

        void TickBrood(float deltaTime)
        {
            AntRole role = NextAntRole ?? AntRole.Worker;
            _brood.LayIntervalFactor = Tree.LayIntervalFactor;
            _brood.EggsPerLay = Tree.EggsPerLay;
            var context = new BroodContext
            {
                Mode = QueenMode, Role = role, Cost = CostOf(role), Nutrients = Nutrients, Adults = _ants.Count,
                AdultCapacity = AdultCapacity, NurserySlots = NurserySlots, QueenX = Queen.X, QueenY = Queen.Y,
                RoleProvider = _nextRoleProvider, CostProvider = _costProvider,
                CareCapacity = _nursery.CapacityInside, Boosts = _boosts,
            };
            _born.Clear();
            Nutrients -= _brood.Tick(deltaTime, context, _born);
            foreach (AntRole born in _born)
                Spawn(born);
            // Quem nasce libera o ponto do berçário que ocupava.
            _nurseryDirty |= _born.Count > 0;
        }

        /// <summary>Restaura a cria e o relógio da postura de um save. Ovos no chão ou no caminho voltam já no berçário.</summary>
        public void RestoreBrood(IEnumerable<BroodRecord> brood, float layTimer)
        {
            _brood.Restore(brood, layTimer);
            _nurseryDirty = true;
        }

        /// <summary>Recalcula o espaço útil (vagas de adulta) e o do berçário a partir da região alcançável do formigueiro.</summary>
        void RecountUsableSpace()
        {
            _usableCells = _colonyField.ReachedCount;
            _nurseryCells = 0;
            _nurseryFloor.Clear();
            _nurserySpots.Clear();
            if (_grid.NurseryRoomCount == 0)
                return;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkNurseryRoomCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (!_grid.IsNurseryRoom(x, y) || _colonyField.Get(x, y) == FlowField.Unreachable)
                        continue;
                    _nurseryCells++;
                    _nurseryFloor.Add(new Vector2Int(x, y));
                }
            }
            PickNurserySpots();
        }

        /// <summary>
        /// Espalha os pontos onde a cria é desenhada por todo o berçário alcançável (chão, paredes e fundo, como as
        /// formigas andam), do mais baixo para o mais alto: cada ponto reserva a largura (<see cref="NurserySpotSpacing"/>)
        /// e a altura (<see cref="NurserySpotHeight"/>) do sprite, então as crias não se sobrepõem.
        /// </summary>
        void PickNurserySpots()
        {
            _nurseryFloor.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            _nurseryTaken.Clear();
            foreach (Vector2Int cell in _nurseryFloor)
            {
                if (_nurseryTaken.Contains(cell))
                    continue;
                _nurserySpots.Add(cell);
                for (int dy = -(NurserySpotHeight - 1); dy < NurserySpotHeight; dy++)
                for (int dx = -(NurserySpotSpacing - 1); dx < NurserySpotSpacing; dx++)
                    _nurseryTaken.Add(new Vector2Int(cell.x + dx, cell.y + dy));
            }
        }

        /// <summary>
        /// Substitui formigas e economia pelas de um save. A grade já precisa estar restaurada. As formigas voltam sem
        /// faixa de despejo escolhida (quem carrega escolhe de novo no próximo passo) e sem animação em andamento.
        /// </summary>
        public void RestoreState(int storedPixels, int totalDelivered, float simulationTime, IEnumerable<Ant> ants,
                                 int drownedAnts = 0)
        {
            DrownedAnts = drownedAnts;
            _builtNutrientVersion = -1;
            // A grade foi trocada de uma vez: nenhum campo vale mais.
            _builtMarkVersion = -1;
            _dirtyUnderground = _dirtySurface = _dirtyStructure = _dirtyNearMarks = true;
            foreach (DumpSide side in _dumpSides)
                side.InFlight = 0;
            _ants.Clear();
            SimulationTime = simulationTime;
            StoredPixels = storedPixels;
            TotalDelivered = totalDelivered;
            foreach (Ant ant in ants)
            {
                // O transporte do ovo não vai para o save (o ovo volta já no berçário): a formiga volta a ficar ociosa.
                if (ant.State == AntState.FetchingEgg || ant.State == AntState.CarryingEgg ||
                    ant.State == AntState.Tending || ant.State == AntState.MovingBrood ||
                    ant.State == AntState.Gardening || ant.State == AntState.MovingNutrient)
                    ant.State = AntState.Idle;
                ant.DumpSide = -1;
                PushOutOfSolid(ant);
                SnapVisual(ant);
                ant.MoveStart = simulationTime;
                _ants.Add(ant);
            }
            _gardenTarget = -1;
            _gardenDirty = true;
        }

        /// <summary>Restaura a economia de nutrientes de um save (depois de <see cref="RestoreState"/>).</summary>
        public void RestoreNutrients(int nutrients, int totalNutrients, int harvestedPixels)
        {
            Nutrients = nutrients;
            TotalNutrients = totalNutrients;
            HarvestedPixels = harvestedPixels;
        }

        /// <summary>Põe a rainha onde o save a deixou (depois de <see cref="RestoreState"/>).</summary>
        public void RestoreQueen(int x, int y, int facingX)
        {
            Queen.X = x;
            Queen.Y = y;
            Queen.FacingX = facingX >= 0 ? 1 : -1;
            Queen.Cooldown = 0f;
            Queen.State = AntState.Idle;
            PushOutOfSolid(Queen);
            SnapVisual(Queen);
            Queen.MoveStart = SimulationTime;
        }

        public void Tick(float deltaTime)
        {
            long stageStart = SimProfiler.Now();
            SimulationTime += deltaTime;
            _activeBuilders = 0;
            _plantsInFlight = 0;
            _fetchers = 0;
            _harvesters = 0;
            _diggers = 0;
            ResetCare();
            _claimed.Clear();
            _claimHash = 0;
            _reserved.Clear();
            foreach (Ant ant in _ants)
            {
                if (ant.State == AntState.Digging)
                    ClaimTarget(ant);
                if (AntRoles.Harvests(ant.Role))
                    _harvesters++;
                if (ant.State == AntState.Gathering || ant.State == AntState.Hauling || ant.State == AntState.Placing)
                    _activeBuilders++;
                if (ant.State == AntState.Placing)
                    _reserved.Add(_grid.ToIndex(ant.DigX, ant.DigY));
                if (IsHarvesting(ant))
                    _plantsInFlight += ant.CarriedCount;
                if (ant.Role == AntRole.Digger)
                    _diggers++;
                CountCare(ant);
                if (ant.State == AntState.Fetching)
                    _fetchers++;
            }
            // Os nutrientes só curam com formiga trabalhando na sala; o ritmo da especialista vale só para o bloco que ela toca.
            ApplyGardenBoosts(deltaTime);
            _grid.AdvanceNutrientCure(deltaTime, GardenCureRate());
            UpdateHarvestPause();
            Profiler.Add(ProfileStage.TickPrep, stageStart);
            RefreshFields(deltaTime);
            stageStart = SimProfiler.Now();
            UpdateQueen(deltaTime);
            Profiler.Add(ProfileStage.Queen, stageStart);
            stageStart = SimProfiler.Now();
            TickBrood(deltaTime);
            Profiler.Add(ProfileStage.Brood, stageStart);
            stageStart = SimProfiler.Now();
            for (int i = 0; i < _ants.Count; i++)
                UpdateAnt(_ants[i], deltaTime);
            Profiler.Add(ProfileStage.Ants, stageStart);
            stageStart = SimProfiler.Now();
            Drown(deltaTime);
            Profiler.Add(ProfileStage.Drown, stageStart);
        }

        /// <summary>
        /// Cheia (sem vaga para mais um pixel) pausa a colheita; ela só reabre quando os guardados passam a menos da metade
        /// da capacidade (guardados &lt; slots livres, pois guardados + livres é a capacidade).
        /// </summary>
        void UpdateHarvestPause()
        {
            if (!_harvestPaused)
                _harvestPaused = !CanHarvest;
            else if (_grid.NutrientCount + _movingNutrients < _freeSlots - _movingNutrients)
                _harvestPaused = false;
        }

        /// <summary>
        /// Conta o tempo seguido de cada formiga debaixo d'água e remove as que passaram de
        /// <see cref="ColonySettings.DrownSeconds"/>. O pixel que ela carregava se perde com ela.
        /// </summary>
        void Drown(float deltaTime)
        {
            for (int i = _ants.Count - 1; i >= 0; i--)
            {
                Ant ant = _ants[i];
                if (!_grid.HasWater(ant.X, ant.Y))
                {
                    ant.UnderwaterSeconds = 0f;
                    continue;
                }
                ant.UnderwaterSeconds += deltaTime;
                if (ant.UnderwaterSeconds < _settings.DrownSeconds)
                    continue;
                ReleaseDumpSide(ant);
                if (ant.State == AntState.FetchingEgg || ant.State == AntState.CarryingEgg || ant.State == AntState.MovingBrood)
                    _brood.ReleaseEgg(ant.EggId, ant.X, ant.Y); // o ovo volta ao chão (ou a cria ao ponto de onde saiu)
                _ants.RemoveAt(i);
                DrownedAnts++;
            }
        }

        // ---------------------------------------------------------------- navegação

        void RefreshFields(float deltaTime)
        {
            _fieldTimer -= deltaTime;
            ScanStructure();
            bool marksChanged = _grid.MarkVersion != _builtMarkVersion;
            // A rainha a caminho da sala muda a origem dos campos de casa (dentro da sala, a origem é a sala toda).
            bool queenMoved = HomeAnchor() != _homeAnchor;
            bool nutrientsChanged = _grid.NutrientVersion != _builtNutrientVersion;
            // Encheu (ou abriu vaga): as plantas marcadas saem (ou voltam) do campo de escavação.
            bool harvestChanged = (CanHarvest, CanStoreDebris, _harvestPaused) != _builtHarvestOpen || RockOpen != _builtRockOpen;
            // Alguém travou ou largou um pixel de planta: os campos precisam deixar de apontar para o travado.
            bool claimsChanged = _claimHash != _builtClaimHash;
            _gardenDirty |= GardenNeedsRefresh(deltaTime) || claimsChanged;

            bool firstBuild = _builtStructureVersion < 0;
            // Só a obra olha qualquer mudança de estrutura (a terra solta de fora é a origem do campo da pilha).
            bool buildStructure = _dirtyStructure && _grid.BuildMarkedCount > 0;
            if (!_dirtyUnderground && !_dirtySurface && !_dirtyNearMarks && !buildStructure && !marksChanged && !queenMoved
                && !nutrientsChanged && !harvestChanged && !_gardenDirty && !_nurseryDirty)
                return;
            // O intervalo limita o custo dos BFS.
            if (_fieldTimer > 0f && !firstBuild)
                return;
            // Vários passos no mesmo frame (velocidade alta ou frame lento): a rodada espera o último, para custar uma por frame.
            if (DeferFieldRefresh && !firstBuild)
                return;

            _fieldTimer = _settings.FieldRebuildInterval;
            Profiler.Count(ProfileCounter.FieldRebuilds);
            FieldRounds++;
            _pendingUnderground = _dirtyUnderground;
            _pendingMarks = marksChanged;
            _pendingQueenMoved = queenMoved;
            CountCauses(_dirtySurface, buildStructure, marksChanged, queenMoved, nutrientsChanged, harvestChanged, claimsChanged);
            // O que foi tratado nesta rodada sai da fila; o que mudar durante as etapas (ou pedir novo cálculo, como
            // TryStartPlace) dispara outra rodada.
            _dirtyUnderground = false;
            _dirtyNearMarks = false;
            _dirtyStructure = false;
            _dirtySurface = false;
            _builtStructureVersion = _grid.StructureVersion;
            _builtMarkVersion = _grid.MarkVersion;
            _builtNutrientVersion = _grid.NutrientVersion;
            _builtClaimHash = _claimHash;
            RunFieldStages();
        }

        void CountCauses(bool surface, bool build, bool marks, bool queen, bool nutrients, bool harvest, bool claims)
        {
            if (_dirtyUnderground) Profiler.Count(ProfileCounter.CauseUnderground);
            if (surface) Profiler.Count(ProfileCounter.CauseSurface);
            if (_dirtyNearMarks) Profiler.Count(ProfileCounter.CauseNearMarks);
            if (build) Profiler.Count(ProfileCounter.CauseBuild);
            if (marks) Profiler.Count(ProfileCounter.CauseMarks);
            if (queen) Profiler.Count(ProfileCounter.CauseQueen);
            if (nutrients) Profiler.Count(ProfileCounter.CauseNutrients);
            if (harvest) Profiler.Count(ProfileCounter.CauseHarvest);
            if (claims) Profiler.Count(ProfileCounter.CauseClaims);
            if (_gardenDirty) Profiler.Count(ProfileCounter.CauseGarden);
            if (_nurseryDirty) Profiler.Count(ProfileCounter.CauseNursery);
        }

        /// <summary>
        /// Quando a estrutura da grade muda, atualiza a caminhabilidade (só os blocos tocados) e anota o que mudou de fato:
        /// túnel, céu e blocos com marca de escavação por perto. As rodadas de campos usam essas anotações em vez de
        /// refazer tudo a cada grão que cai.
        /// </summary>
        void ScanStructure()
        {
            if (_grid.StructureVersion == _scannedStructureVersion)
                return;
            _scannedStructureVersion = _grid.StructureVersion;
            _dirtyStructure = true;
            _walkability.Refresh();
            _walkability.TakeChanges(out bool underground, out bool surface);
            _dirtyUnderground |= underground;
            _dirtySurface |= surface;
            for (int cy = 0; cy < _grid.ChunkRows; cy++)
            for (int cx = 0; cx < _grid.ChunkColumns; cx++)
            {
                int c = cy * _grid.ChunkColumns + cx;
                int version = _grid.GetChunkVersion(cx, cy);
                if (_chunkSeen[c] == version)
                    continue;
                _chunkSeen[c] = version;
                _dirtyNearMarks |= _grid.MarkedCount > 0 && HasMarksAround(cx, cy);
            }
        }

        // Etapas de uma rodada de campos, medidas à parte (painel F11). Rodam todas no mesmo passo: espalhá-las por vários
        // passos deixa campos velhos em uso e quebra testes de ordem de obra e de equilíbrio do despejo.
        const int FieldStageCount = 9;
        // O que mudou desde a última rodada (acumulado entre elas) e o que a rodada em curso trata.
        bool _dirtyUnderground = true, _dirtySurface = true, _dirtyStructure = true, _dirtyNearMarks = true;
        int _scannedStructureVersion = -1;
        readonly int[] _chunkSeen;
        bool _pendingUnderground, _pendingMarks, _pendingQueenMoved;

        void RunFieldStages()
        {
            long fieldsStart = SimProfiler.Now();
            for (int stage = 0; stage < FieldStageCount; stage++)
            {
                long stageStart = SimProfiler.Now();
                Profiler.Add(RunFieldStage(stage), stageStart);
            }
            // Todos os campos já refletem o estado atual: a lista de mudanças recomeça.
            _walkability.ClearChangedCells();
            Profiler.Add(ProfileStage.Fields, fieldsStart);
        }

        // Todas as etapas rodam em toda rodada: cada campo corrige só o que mudou desde a anterior (FlowField.Update), e para isso
        // precisa ver todas as mudanças de caminhabilidade registradas. Só o que não é campo (inundação da sala da rainha, região
        // do formigueiro) espera o túnel, as marcas ou a rainha mudarem.
        ProfileStage RunFieldStage(int stage)
        {
            switch (stage)
            {
                case 0:
                    _walkability.Refresh();
                    return ProfileStage.FieldWalkability;
                case 1:
                    if (_pendingUnderground || _pendingMarks)
                        UpdateQueenRoom();
                    UpdateField(_queenRoomField, _roomCells);
                    return ProfileStage.FieldQueenRoom;
                case 2:
                    BuildDumpField();
                    return ProfileStage.FieldDump;
                case 3:
                    BuildHomeField();
                    return ProfileStage.FieldHome;
                case 4:
                    // Depois do campo de casa: ele refaz os pontos do berçário (RecountUsableSpace).
                    BuildNurseryField();
                    return ProfileStage.FieldNursery;
                case 5:
                    BuildNutrientFields();
                    return ProfileStage.FieldNutrients;
                case 6:
                    BuildDigField();
                    return ProfileStage.FieldDig;
                case 7:
                    // Depois dos campos dos slots: o alvo só vale se a sala de nutrientes for alcançável de onde se corta.
                    BuildGardenField();
                    return ProfileStage.FieldGarden;
                default:
                    BuildConstructionFields();
                    return ProfileStage.FieldConstruction;
            }
        }

        /// <summary>Corrige o campo no que mudou; conta as vezes em que teve de refazer do zero (painel F11).</summary>
        void UpdateField(FlowField field, List<int> sources)
        {
            field.Update(_walkability, sources);
            if (!field.LastUpdateWasIncremental)
                Profiler.Count(ProfileCounter.FieldFullBuilds);
        }

        void BuildDigField()
        {
            CollectMarkedSources();
            _builtHarvestOpen = (CanHarvest, CanStoreDebris, _harvestPaused);
            UpdateField(_digField, _sources);

            _builtRockOpen = RockOpen;
            if (_builtRockOpen)
                CollectMarkedSources(rock: true);
            else
                _sources.Clear();
            UpdateField(_rockField, _sources);
        }

        /// <summary>Há escavadora viva e E2 comprada: a rocha marcada entra no trabalho.</summary>
        bool RockOpen => _diggers > 0 && Tree.Owns(QueenNode.E2);

        /// <summary>Campo das jardineiras: a linha de cima da planta-alvo, sem os pixels que outra formiga já travou.</summary>
        void BuildGardenField()
        {
            _gardenDirty = false;
            if (_vegetation != null && _harvesters > 0 && _gardenTarget < 0)
                PickGardenTarget();
            _sources.Clear();
            if (_gardenTarget >= 0)
            {
                int w = _grid.Width;
                for (int x = _gardenMinX; x <= _gardenMaxX; x++)
                {
                    if (!IsPlantTarget(x, _gardenRow))
                        continue;
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k], ny = _gardenRow + AntNavigation.Dy[k];
                        if (_grid.InBounds(nx, ny) && _walkability.IsWalkableAt(ny * w + nx) &&
                            _storeField.Get(nx, ny) != FlowField.Unreachable)
                            _sources.Add(ny * w + nx);
                    }
                }
            }
            UpdateField(_gardenField, _sources);
        }

        /// <summary>
        /// Origens das comuns = células caminháveis com algum vizinho marcado que dá para pegar: terra escavável ou
        /// pixel solto de planta (as jardineiras só cortam; quem recolhe os pixels são as comuns). Procura pelo lado caminhável (poucas
        /// células, só nos blocos com marca ou encostados neles), não pelo marcado (a área marcada costuma ser grande e
        /// quase toda maciça). A distância do BFS não depende da ordem das origens, então o campo é o mesmo.
        /// </summary>
        void CollectMarkedSources(bool rock = false)
        {
            _sources.Clear();
            if (_grid.MarkedCount == 0)
                return;
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
                        int tx = x + AntNavigation.Dx[k], ty = y + AntNavigation.Dy[k];
                        if (rock ? !IsRockTarget(tx, ty) : !IsWorkTarget(tx, ty, x, y))
                            continue;
                        _sources.Add(y * w + x);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Pixel marcado que a comum em (<paramref name="standX"/>, <paramref name="standY"/>) pode pegar: terra escavável
        /// ou pixel solto de planta (colhido onde parou). O solto pede slot livre e sala de nutrientes alcançável de onde
        /// ela está, para não pegar o que ninguém guardará, e não pode estar travado por outra formiga.
        /// </summary>
        bool IsWorkTarget(int x, int y, int standX, int standY)
        {
            if (!_grid.IsMarked(x, y))
                return false;
            CellMaterial material = _grid.Get(x, y);
            if (CellMaterials.IsDiggable(material))
                return true;
            return CellMaterials.IsPlantDebris(material) && CanStoreDebris && !_claimed.Contains(_grid.ToIndex(x, y)) &&
                   _storeField.Get(standX, standY) != FlowField.Unreachable;
        }

        /// <summary>Rocha marcada que a escavadora pode quebrar, sem outra já quebrando a mesma.</summary>
        bool IsRockTarget(int x, int y) =>
            _grid.IsMarked(x, y) && _grid.Get(x, y) == CellMaterial.Rock && !_claimed.Contains(_grid.ToIndex(x, y));

        /// <summary>Trava o pixel que a formiga está cortando/pegando, se for de planta (em pé ou solto) ou rocha.</summary>
        void ClaimTarget(Ant ant)
        {
            CellMaterial material = _grid.Get(ant.DigX, ant.DigY);
            if (!CellMaterials.IsPlant(material) && !CellMaterials.IsPlantDebris(material) && material != CellMaterial.Rock)
                return;
            int index = _grid.ToIndex(ant.DigX, ant.DigY);
            if (_claimed.Add(index))
                _claimHash += index * 2654435761L;
        }

        /// <summary>Pixel cortável da linha de cima da planta-alvo (corta-se sempre de cima para baixo).</summary>
        bool IsPlantTarget(int x, int y) =>
            _gardenTarget >= 0 && y == _gardenRow && x >= _gardenMinX && x <= _gardenMaxX && CanHarvest && !_harvestPaused &&
            VegetationSimulation.CanHarvest(_grid, x, y) && !_claimed.Contains(_grid.ToIndex(x, y));

        /// <summary>
        /// A planta-alvo mudou de linha, acabou ou ainda não existe: o campo das jardineiras precisa ser refeito. Sem alvo,
        /// tenta escolher outro a cada <see cref="GardenRepickSeconds"/>.
        /// </summary>
        bool GardenNeedsRefresh(float deltaTime)
        {
            if (_vegetation == null || _harvesters == 0)
            {
                if (_gardenTarget < 0)
                    return false;
                _gardenTarget = -1;
                return true;
            }
            if (_gardenTarget >= 0)
            {
                if (_vegetation.TryGetHarvestRow(_grid, _gardenTarget, out int row, out int minX, out int maxX))
                {
                    if (row == _gardenRow && minX == _gardenMinX && maxX == _gardenMaxX)
                        return false;
                    _gardenRow = row;
                    _gardenMinX = minX;
                    _gardenMaxX = maxX;
                    return true;
                }
                _gardenTarget = -1; // nada mais a cortar: escolhe outra planta já
                return true;
            }
            _gardenRepickTimer -= deltaTime;
            if (_gardenRepickTimer > 0f)
                return false;
            _gardenRepickTimer = GardenRepickSeconds;
            return true;
        }

        readonly List<Vector3Int> _gardenCandidates = new List<Vector3Int>();

        /// <summary>
        /// Escolhe a planta-alvo: entre as que cresceram o mínimo (<see cref="VegetationSettings.MinHarvestGrowth"/>) e
        /// têm caminho até a sala de nutrientes, a maior; no empate, a mais perto da entrada do formigueiro. Ela fica
        /// como alvo até não restar o que cortar.
        /// </summary>
        void PickGardenTarget()
        {
            _gardenCandidates.Clear();
            foreach (int rootX in _vegetation.Plants)
            {
                if (_vegetation.IsHarvestable(_grid, rootX, out int rows))
                    _gardenCandidates.Add(new Vector3Int(rootX, rows, Mathf.Abs(rootX - _layout.EntranceX)));
            }
            _gardenCandidates.Sort((a, b) => a.y != b.y ? b.y.CompareTo(a.y) : a.z != b.z ? a.z.CompareTo(b.z) : a.x.CompareTo(b.x));
            foreach (Vector3Int c in _gardenCandidates)
            {
                if (!_vegetation.TryGetHarvestRow(_grid, c.x, out int row, out int minX, out int maxX) ||
                    !RowReachable(row, minX, maxX))
                    continue;
                _gardenTarget = c.x;
                _gardenRow = row;
                _gardenMinX = minX;
                _gardenMaxX = maxX;
                return;
            }
        }

        /// <summary>Algum pixel cortável da linha tem uma célula caminhável ao lado, de onde a sala de nutrientes se alcança.</summary>
        bool RowReachable(int row, int minX, int maxX)
        {
            int w = _grid.Width;
            for (int x = minX; x <= maxX; x++)
            {
                if (!VegetationSimulation.CanHarvest(_grid, x, row))
                    continue;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k], ny = row + AntNavigation.Dy[k];
                    if (_grid.InBounds(nx, ny) && _walkability.IsWalkableAt(ny * w + nx) &&
                        _storeField.Get(nx, ny) != FlowField.Unreachable)
                        return true;
                }
            }
            return false;
        }

        /// <summary>Quanto o papel encurta (ou não) o tempo de escavar, construir ou cortar.</summary>
        float WorkTimeFactor(Ant ant) =>
            AntRoles.DigTimeFactor(ant.Role) * Tree.DigTimeFactor(ant.Role) * AntRoles.CutTimeFactor(ant.Role) *
            Tree.CutTimeFactor(ant.Role);

        /// <summary>Leva planta na mão (cada pixel ocupa um slot da sala de nutrientes até ser guardado). O pixel solto, no chão, reserva via PlantDebrisCount.</summary>
        bool IsHarvesting(Ant ant) => CellMaterials.IsPlant(ant.Carried);

        // ---------------------------------------------------------------- nutrientes

        /// <summary>
        /// Campo até os slots livres da sala de nutrientes (células pintadas, caminháveis e sem nutriente) e campo até
        /// os nutrientes guardados prontos (os que ainda curam ocupam o slot, mas não são buscados). Conta os slots e vê se algum nutriente tem caminho até a rainha.
        /// </summary>
        void BuildNutrientFields()
        {
            _sources.Clear();
            _buildSources.Clear();
            _curingSources.Clear();
            if (_grid.NutrientRoomCount > 0 || _grid.NutrientCount > 0)
            {
                int w = _grid.Width;
                RectInt area = _grid.Active;
                int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
                int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
                for (int cy = cyMin; cy <= cyMax; cy++)
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    if (_grid.GetChunkNutrientRoomCount(cx, cy) == 0 && _grid.GetChunkNutrientCount(cx, cy) == 0)
                        continue;
                    int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                    int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                    int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                    int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        int i = y * w + x;
                        if (!_walkability.IsWalkableAt(i))
                            continue;
                        // Pixel ainda curando ocupa o slot, mas não é buscado até virar nutriente.
                        if (_grid.HasNutrient(x, y))
                        {
                            if (_grid.HasReadyNutrient(x, y))
                                _buildSources.Add(i);
                            else
                                _curingSources.Add(i);
                        }
                        else if (_grid.IsNutrientRoom(x, y))
                            _sources.Add(i);
                    }
                }
            }
            _freeSlotCells.Clear();
            _freeSlotCells.AddRange(_sources);
            UpdateField(_storeField, _sources);
            UpdateField(_nutrientField, _buildSources);
            UpdateField(_curingField, _curingSources);

            // Se o slot é alcançável de onde a planta é colhida, IsWorkTarget confere pelo campo dos slots.
            int width = _grid.Width;
            _freeSlots = _sources.Count;
            _nutrientsReachQueen = false;
            foreach (int i in _buildSources)
                _nutrientsReachQueen |= _homeField.Get(i % width, i / width) != FlowField.Unreachable;
            // _buildSources é da construção: devolve vazio (BuildConstructionFields o refaz quando precisa).
            _buildSources.Clear();
        }

        /// <summary>
        /// Carregando um pixel de planta: vai até um slot livre e o guarda lá. O pixel cura no slot por
        /// <see cref="ColonySettings.NutrientCureSeconds"/> antes de virar nutriente que pode ser levado à rainha.
        /// </summary>
        void UpdateStoring(Ant ant)
        {
            int d = _storeField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                if (!TryStore(ant))
                    Hold(ant); // o slot foi ocupado por outra formiga: o próximo recálculo manda esta para outro
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _storeField))
                return;
            // Sala cheia, apagada ou sem caminho: espera com a planta na mão perto de casa até abrir vaga.
            WanderHome(ant);
        }

        bool TryStore(Ant ant)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                if (!_grid.IsNutrientRoom(x, y) || _grid.HasNutrient(x, y) || !AntNavigation.IsWalkable(_grid, x, y))
                    continue;
                _grid.SetNutrient(x, y, true, _settings.NutrientCureSeconds);
                _freeSlots = Mathf.Max(0, _freeSlots - 1);
                _plantsInFlight = Mathf.Max(0, _plantsInFlight - 1);
                HarvestedPixels++;
                ant.DropTop();
                // Ainda tem planta na mão: segue guardando (um pixel por vez) antes de voltar ao trabalho.
                ant.State = ant.CarriedCount > 0 ? AntState.Carrying : AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                return true;
            }
            return false;
        }

        /// <summary>Teto de formigas buscando nutrientes ao mesmo tempo (o resto continua cavando e colhendo).</summary>
        int MaxFetchers => Mathf.Max(1, _ants.Count / 2);

        /// <summary>Ociosa: se houver nutriente guardado com caminho até a rainha e ninguém indo buscá-lo, vai buscar.</summary>
        bool TryStartFetch(Ant ant)
        {
            // A carregadora busca mesmo com o teto de buscadoras cheio: transportar é o trabalho dela.
            bool capped = _fetchers >= MaxFetchers && ant.Role != AntRole.Carrier;
            if (!_nutrientsReachQueen || _fetchers >= _grid.ReadyNutrientCount || capped)
                return false;
            if (_nutrientField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.State = AntState.Fetching;
            _fetchers++;
            UpdateFetching(ant);
            return true;
        }

        void UpdateFetching(Ant ant)
        {
            int d = _nutrientField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                if (TryPickNutrient(ant))
                    return;
                // Outra formiga levou este: volta a ficar ociosa e escolhe de novo.
                ant.State = AntState.Idle;
                Hold(ant);
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _nutrientField))
                return;
            ant.State = AntState.Idle;
            Hold(ant);
        }

        bool TryPickNutrient(Ant ant)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                if (!_grid.HasReadyNutrient(x, y))
                    continue;
                _grid.SetNutrient(x, y, false);
                ant.State = AntState.Delivering;
                ant.Cooldown = _settings.DropSeconds;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Leva o nutriente até a rainha: desce o campo de casa (até a sala dela, ou até ela fora da sala) e, dentro da
        /// sala, chega perto dela. Encostou (8 vizinhas), ela consome: +1 nutriente.
        /// </summary>
        void UpdateDelivering(Ant ant)
        {
            if (Mathf.Abs(ant.X - Queen.X) <= 1 && Mathf.Abs(ant.Y - Queen.Y) <= 1)
            {
                Nutrients++;
                TotalNutrients++;
                ant.State = AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                return;
            }
            WalkHomeTowards(ant, Queen.X, Queen.Y);
        }

        /// <summary>
        /// Um passo rumo à célula (<paramref name="x"/>, <paramref name="y"/>) perto da rainha: desce o campo de casa até a
        /// sala dela (ou até ela, fora da sala) e, dentro, aproxima-se da célula pelo tabuleiro.
        /// </summary>
        void WalkHomeTowards(Ant ant, int x, int y)
        {
            int d = _homeField.Get(ant.X, ant.Y);
            if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, _homeField))
                return;
            if (d == 0 && StepTowardsCell(ant, x, y))
                return;
            if (d == 0 && QueenInRoom)
            {
                Wander(ant, roomLabel: _queenLabel);
                return;
            }
            if (d == FlowField.Unreachable)
                Wander(ant);
            else
                Hold(ant);
        }

        // ---------------------------------------------------------------- ovo

        /// <summary>
        /// Ociosa (operária ou carregadora): se a rainha pôs um ovo que ninguém foi buscar e há berçário com ponto de chão
        /// alcançável, vai buscá-lo.
        /// </summary>
        bool TryStartEggFetch(Ant ant)
        {
            if (_nurserySpots.Count == 0 || !_brood.HasEggToCarry || _homeField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            if (!_brood.TryClaimEgg(out int id))
                return false;
            ant.EggId = id;
            ant.State = AntState.FetchingEgg;
            UpdateFetchingEgg(ant);
            return true;
        }

        void UpdateFetchingEgg(Ant ant)
        {
            if (!_brood.TryGet(ant.EggId, out BroodRecord egg) || egg.Phase != EggPhase.Claimed)
            {
                ant.State = AntState.Idle;
                Hold(ant);
                return;
            }
            if (Mathf.Abs(ant.X - egg.LaidX) <= 1 && Mathf.Abs(ant.Y - egg.LaidY) <= 1)
            {
                _brood.PickUpEgg(ant.EggId);
                ant.State = AntState.CarryingEgg;
                ant.Cooldown = _settings.DropSeconds;
                return;
            }
            WalkHomeTowards(ant, egg.LaidX, egg.LaidY);
        }

        /// <summary>
        /// Leva o ovo até um dos pontos livres do berçário (<see cref="BuildNurseryField"/>) e o larga ali: a partir daí ele
        /// se desenvolve. Sem berçário alcançável, espera por perto com o ovo na boca.
        /// </summary>
        void UpdateCarryingEgg(Ant ant)
        {
            if (!_brood.TryGet(ant.EggId, out BroodRecord egg) || egg.Phase != EggPhase.Carried)
            {
                ant.State = AntState.Idle;
                Hold(ant);
                return;
            }
            int d = _nurseryField.Get(ant.X, ant.Y);
            if (d == 0)
            {
                int slot = SpotIndexAt(ant.X, ant.Y);
                // Outro ovo pode ter ocupado este ponto desde o último cálculo do campo: espera o campo apontar para outro.
                if (slot < 0 || _brood.PlacedAt(slot) > _nurseryMinLoad)
                {
                    _nurseryDirty = true;
                    Hold(ant);
                    return;
                }
                _brood.PlaceEgg(ant.EggId, slot);
                _nurseryDirty = true;
                ant.State = AntState.Idle;
                ant.Cooldown = _settings.DropSeconds;
                return;
            }
            if (d != FlowField.Unreachable && StepDownhill(ant, _nurseryField))
                return;
            WanderHome(ant);
        }

        int SpotIndexAt(int x, int y)
        {
            for (int i = 0; i < _nurserySpots.Count; i++)
            {
                if (_nurserySpots[i].x == x && _nurserySpots[i].y == y)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Campo até os pontos do berçário com menos crias (todos, se estiverem empatados): o ovo vai para um ponto livre e só
        /// divide ponto quando não há mais livre.
        /// </summary>
        void BuildNurseryField()
        {
            _nurseryDirty = false;
            _sources.Clear();
            _nurseryMinLoad = int.MaxValue;
            for (int i = 0; i < _nurserySpots.Count; i++)
                _nurseryMinLoad = Mathf.Min(_nurseryMinLoad, _brood.PlacedAt(i));
            for (int i = 0; i < _nurserySpots.Count; i++)
            {
                if (_brood.PlacedAt(i) != _nurseryMinLoad)
                    continue;
                int index = _grid.ToIndex(_nurserySpots[i].x, _nurserySpots[i].y);
                if (_walkability.IsWalkableAt(index))
                    _sources.Add(index);
            }
            UpdateField(_nurseryField, _sources);
            _sources.Clear();
        }

        /// <summary>Um passo que aproxima da célula (distância de tabuleiro), sem sair da sala da rainha quando ela está nela.</summary>
        bool StepTowardsCell(Ant ant, int x, int y, bool keepQueenRoom = true)
        {
            int best = Mathf.Max(Mathf.Abs(ant.X - x), Mathf.Abs(ant.Y - y));
            int chosen = -1;
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (!AntNavigation.IsWalkable(_grid, nx, ny) || keepQueenRoom && QueenInRoom && !IsQueenArea(nx, ny))
                    continue;
                int distance = Mathf.Max(Mathf.Abs(nx - x), Mathf.Abs(ny - y));
                if (distance < best)
                {
                    best = distance;
                    chosen = k;
                }
            }
            if (chosen < 0)
                return false;
            MoveTo(ant, chosen);
            return true;
        }

        // ---------------------------------------------------------------- construção

        void BuildConstructionFields()
        {
            _buildSources.Clear();
            _siteSources.Clear();
            _hasBuildWork = false;
            _hasSite = false;
            if (_grid.BuildMarkedCount == 0)
            {
                // Parados, os campos deixam de acompanhar as mudanças de caminhabilidade: refazem do zero quando voltar a obra.
                _exitField.Invalidate();
                _pileField.Invalidate();
                _buildField.Invalidate();
                _siteField.Invalidate();
                _buildFieldsVersion = -1;
                return;
            }

            // Saída e terra solta só mudam com a estrutura; as marcas e as reservas mudam o resto.
            if (_buildFieldsVersion != _grid.StructureVersion)
            {
                BuildExitField();
                BuildPileField();
                _buildFieldsVersion = _grid.StructureVersion;
            }

            int w = _grid.Width;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkBuildMarkedCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (!_grid.IsBuildMarked(x, y) || _grid.IsSolid(x, y))
                        continue;
                    int d = _exitField.Get(x, y);
                    if (d == FlowField.Unreachable)
                        continue;
                    bool fillable = IsFillable(x, y, true);
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k];
                        int ny = y + AntNavigation.Dy[k];
                        // A formiga encaixa de um ponto mais perto da saída, para ficar do lado de fora do que preencheu.
                        if (!_walkability.IsWalkable(nx, ny))
                            continue;
                        // Dentro: de um ponto mais perto da saída. Fora: de qualquer ponto ligado à superfície.
                        int nd = _exitField.Get(nx, ny);
                        if (IsOutdoor(x, y) ? nd == FlowField.Unreachable : nd >= d)
                            continue;
                        if (fillable)
                            _buildSources.Add(ny * w + nx);
                        // Fila de espera: fora das marcas, para a formiga com terra não ficar em cima do que vai ser preenchido.
                        if (!_grid.IsBuildMarked(nx, ny))
                            _siteSources.Add(ny * w + nx);
                    }
                }
            }
            UpdateField(_buildField, _buildSources);
            UpdateField(_siteField, _siteSources);
            _hasBuildWork = _buildSources.Count > 0;
            _hasSite = _hasBuildWork || _siteSources.Count > 0;
        }

        /// <summary>Raio (em células) em que uma marca mais funda segura o preenchimento de uma mais rasa.</summary>
        const int BuildOrderRadius = 2;
        /// <summary>Quanto mais funda (distância até a saída) pode ser uma marca vizinha sem segurar a célula: dá vários pontos de trabalho de uma vez.</summary>
        const int BuildOrderSlack = 1;
        /// <summary>Teto de células da varredura de bolsão; acima disso o espaço conta como aberto.</summary>
        const int PocketScanLimit = 1500;

        /// <summary>
        /// Célula marcada que pode ser preenchida agora: alcançável pela saída; sem marca vizinha bem mais funda (o
        /// preenchimento vai do fundo para fora); sem outra formiga já indo para ela (<paramref name="respectReserved"/>);
        /// e sem fechar a passagem até outra marca de construção (<see cref="BlocksPassage"/>).
        /// </summary>
        bool IsFillable(int x, int y, bool respectReserved)
        {
            if (!_grid.IsBuildMarked(x, y) || _grid.IsSolid(x, y))
                return false;
            int d = _exitField.Get(x, y);
            if (d == FlowField.Unreachable)
                return false;
            if (respectReserved && _reserved.Contains(_grid.ToIndex(x, y)))
                return false;
            // Do lado de fora a obra cresce a partir do que já existe: a célula só é alcançável (d finito) quando encosta
            // em algo sólido, então o apoio vem de graça e a ordem de baixo para cima também. A regra de profundidade
            // vale só dentro do formigueiro.
            bool outdoor = IsOutdoor(x, y);
            for (int dy = -BuildOrderRadius; dy <= BuildOrderRadius && !outdoor; dy++)
            for (int dx = -BuildOrderRadius; dx <= BuildOrderRadius; dx++)
            {
                if (!_grid.IsBuildMarked(x + dx, y + dy) || IsOutdoor(x + dx, y + dy))
                    continue;
                int nd = _exitField.Get(x + dx, y + dy);
                if (nd != FlowField.Unreachable && nd > d + BuildOrderSlack)
                    return false;
            }
            return !BlocksPassage(x, y);
        }

        /// <summary>
        /// Preencher (x, y) isolaria algum espaço que ainda tem marca de construção? Olha as 8 vizinhas vazias: se
        /// ficarem em um grupo só (movimento em 8 direções), nada se isola. Se houver mais de um grupo, cada um que não
        /// é o da saída é varrido (limitado): se não achar caminho até o grupo da saída e tiver marca dentro, bloqueia.
        /// </summary>
        bool BlocksPassage(int x, int y)
        {
            int n = 0;
            for (int k = 0; k < 8; k++)
            {
                int nx = x + AntNavigation.Dx[k];
                int ny = y + AntNavigation.Dy[k];
                if (!_grid.InBounds(nx, ny) || _grid.IsSolid(nx, ny))
                    continue;
                _ringX[n] = nx;
                _ringY[n] = ny;
                _ringRoot[n] = n;
                n++;
            }
            if (n <= 1)
                return false;

            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                if (Mathf.Abs(_ringX[i] - _ringX[j]) > 1 || Mathf.Abs(_ringY[i] - _ringY[j]) > 1)
                    continue;
                int a = RingRoot(i), b = RingRoot(j);
                if (a != b)
                    _ringRoot[b] = a;
            }

            int main = -1, mainDistance = FlowField.Unreachable;
            bool split = false;
            for (int i = 0; i < n; i++)
            {
                if (RingRoot(i) != RingRoot(0))
                    split = true;
                int d = _exitField.Get(_ringX[i], _ringY[i]);
                if (d < mainDistance)
                {
                    mainDistance = d;
                    main = i;
                }
            }
            if (!split)
                return false;
            if (main < 0)
                return true;

            int mainRoot = RingRoot(main);
            for (int i = 0; i < n; i++)
            {
                int root = RingRoot(i);
                if (root == mainRoot)
                    continue;
                // Só varre uma vez por grupo (a primeira célula dele).
                bool first = true;
                for (int j = 0; j < i; j++)
                    first &= RingRoot(j) != root;
                if (first && PocketHasBuildMarks(x, y, i, mainRoot, n))
                    return true;
            }
            return false;
        }

        int RingRoot(int i)
        {
            while (_ringRoot[i] != i)
                i = _ringRoot[i];
            return i;
        }

        /// <summary>
        /// Varre o espaço vazio a partir da célula <paramref name="start"/> do anel, sem passar por (blockX, blockY).
        /// Devolve true se ficar preso sem chegar ao grupo da saída e houver marca de construção lá dentro.
        /// </summary>
        bool PocketHasBuildMarks(int blockX, int blockY, int start, int mainRoot, int ringCount)
        {
            int w = _grid.Width;
            int stamp = ++_floodCounter;
            int head = 0, tail = 0;
            int blocked = blockY * w + blockX;
            int first = _ringY[start] * w + _ringX[start];
            _floodStamp[first] = stamp;
            _floodQueue[tail++] = first;
            bool marks = false;

            while (head < tail)
            {
                int i = _floodQueue[head++];
                int x = i % w, y = i / w;
                if (_grid.IsBuildMarked(x, y))
                    marks = true;
                for (int r = 0; r < ringCount; r++)
                {
                    if (RingRoot(r) == mainRoot && _ringX[r] == x && _ringY[r] == y)
                        return false; // ligado ao grupo da saída por outro caminho
                }
                if (tail >= PocketScanLimit)
                    return false; // espaço grande: conta como aberto
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k];
                    int ny = y + AntNavigation.Dy[k];
                    if (!_grid.InBounds(nx, ny) || _grid.IsSolid(nx, ny))
                        continue;
                    int ni = ny * w + nx;
                    if (ni == blocked || _floodStamp[ni] == stamp)
                        continue;
                    _floodStamp[ni] = stamp;
                    _floodQueue[tail++] = ni;
                }
            }
            return marks;
        }

        /// <summary>Distância até a superfície: origens = a célula logo acima do solo de cada coluna (ou a da abertura).</summary>
        void BuildExitField()
        {
            _sources.Clear();
            RectInt area = _grid.Active;
            for (int x = area.xMin; x < area.xMax; x++)
            {
                int surface = _grid.GetSurfaceY(x);
                if (_walkability.IsWalkable(x, surface + 1))
                    _sources.Add(_grid.ToIndex(x, surface + 1));
                else if (_walkability.IsWalkable(x, surface))
                    _sources.Add(_grid.ToIndex(x, surface));
            }
            UpdateField(_exitField, _sources);
        }

        /// <summary>Origens = células caminháveis encostadas em terra solta acima do solo original (a pilha de fora).</summary>
        void BuildPileField()
        {
            _sources.Clear();
            RectInt area = _grid.Active;
            int w = _grid.Width;
            for (int x = area.xMin; x < area.xMax; x++)
            {
                for (int y = Mathf.Max(area.yMin, _grid.GetSurfaceY(x) + 1); y < area.yMax && IsPileCell(x, y); y++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k];
                        int ny = y + AntNavigation.Dy[k];
                        if (_walkability.IsWalkable(nx, ny))
                            _sources.Add(ny * w + nx);
                    }
                }
            }
            UpdateField(_pileField, _sources);
        }

        /// <summary>Acima do solo original da coluna: o céu, onde também se constrói.</summary>
        bool IsOutdoor(int x, int y) => y > _grid.GetSurfaceY(x);

        /// <summary>
        /// Terra solta que dá para pegar: grão solto (terra despejada, areia, lama) acima do solo original da coluna.
        /// A terra compactada fica de fora, senão as formigas desmontariam a obra de fora para construir.
        /// </summary>
        bool IsPileCell(int x, int y)
        {
            if (!IsOutdoor(x, y))
                return false;
            CellMaterial material = _grid.Get(x, y);
            return material == CellMaterial.Spoil || material == CellMaterial.Sand || material == CellMaterial.Mud;
        }

        bool AntAt(int x, int y)
        {
            if (Queen.X == x && Queen.Y == y)
                return true;
            foreach (Ant ant in _ants)
            {
                if (ant.X == x && ant.Y == y)
                    return true;
            }
            return false;
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
            bool sidesChanged = UpdateDumpSides();
            foreach (DumpSide side in _dumpSides)
            {
                _sources.Clear();
                for (int x = side.MinX; x <= side.MaxX; x++)
                for (int y = Mathf.Max(0, _grid.GetSurfaceY(x) + 1); y < _grid.Height; y++)
                {
                    if (_walkability.IsWalkable(x, y))
                        _sources.Add(_grid.ToIndex(x, y));
                }
                UpdateField(side.Field, _sources);
            }

            // Faixas novas: quem já carregava escolhe de novo, agora com os campos prontos.
            if (sidesChanged)
            {
                foreach (Ant ant in _ants)
                {
                    if (ant.State == AntState.Carrying && !CellMaterials.IsPlant(ant.Carried))
                        AssignDumpSide(ant);
                }
            }
        }

        // ---------------------------------------------------------------- despejo

        /// <summary>
        /// Recalcula as faixas de despejo se as aberturas na superfície mudaram. Abertura = colunas vizinhas cuja célula
        /// do solo original (<see cref="PixelGrid.GetSurfaceY"/>) foi escavada: o túnel chega ao céu ali. Cada abertura
        /// ganha uma faixa de cada lado, com a mesma distância e largura que a faixa configurada tem da entrada; sem
        /// nenhuma abertura, valem a faixa configurada e o espelho dela. Devolve true se as faixas mudaram.
        /// </summary>
        bool UpdateDumpSides()
        {
            RectInt area = _grid.Active;
            ScanOpenings(area, _scannedOpenings);
            if (_dumpSides.Count > 0 && area.xMin == _sidesArea.xMin && area.xMax == _sidesArea.xMax &&
                SameOpenings(_scannedOpenings, _allOpenings))
                return false;

            // Os índices antigos deixam de valer; as faixas novas começam sem ninguém a caminho.
            foreach (Ant ant in _ants)
                ant.DumpSide = -1;
            _sidesArea = area;
            _allOpenings.Clear();
            _allOpenings.AddRange(_scannedOpenings);
            foreach (DumpSide side in _dumpSides)
                _fieldPool.Add(side.Field);
            _dumpSides.Clear();
            _dumpStrips.Clear();
            _openings.Clear();

            if (_scannedOpenings.Count == 0)
            {
                AddDumpSide(0, _layout.DumpMinX, _layout.DumpMaxX, area);
                if (_layout.TryGetMirrorDump(_grid.Width, out int mirrorMin, out int mirrorMax))
                    AddDumpSide(0, mirrorMin, mirrorMax, area);
                return true;
            }

            // Muitas aberturas = muitos campos de navegação: fica com as mais perto da entrada.
            _openings.AddRange(_scannedOpenings);
            int entrance = _layout.EntranceX;
            _openings.Sort((a, b) => Mathf.Abs(a.x + a.y - 2 * entrance).CompareTo(Mathf.Abs(b.x + b.y - 2 * entrance)));
            if (_openings.Count > _settings.MaxDumpOpenings)
                _openings.RemoveRange(_settings.MaxDumpOpenings, _openings.Count - _settings.MaxDumpOpenings);

            GetDumpOffsets(out int gap, out int length);
            for (int i = 0; i < _openings.Count; i++)
            {
                Vector2Int opening = _openings[i];
                int leftMin = opening.x - gap - length, leftMax = opening.x - gap;
                int rightMin = opening.y + gap, rightMax = opening.y + gap + length;
                // Não despeja em cima de outra abertura (nem das que ficaram sem faixa): a terra taparia o buraco.
                foreach (Vector2Int other in _allOpenings)
                {
                    if (other.y < opening.x)
                        leftMin = Mathf.Max(leftMin, other.y + OpeningClearance + 1);
                    else if (other.x > opening.y)
                        rightMax = Mathf.Min(rightMax, other.x - OpeningClearance - 1);
                }
                AddDumpSide(i, leftMin, leftMax, area);
                AddDumpSide(i, rightMin, rightMax, area);
            }
            return true;
        }

        const int OpeningClearance = 2;

        void ScanOpenings(RectInt area, List<Vector2Int> result)
        {
            result.Clear();
            int start = -1;
            for (int x = area.xMin; x <= area.xMax; x++)
            {
                bool open = x < area.xMax && IsOpenColumn(x, area);
                if (open && start < 0)
                {
                    start = x;
                }
                else if (!open && start >= 0)
                {
                    result.Add(new Vector2Int(start, x - 1));
                    start = -1;
                }
            }
        }

        bool IsOpenColumn(int x, RectInt area)
        {
            int y = _grid.GetSurfaceY(x);
            return y >= area.yMin && y < area.yMax && _grid.Get(x, y) == CellMaterial.Empty;
        }

        static bool SameOpenings(List<Vector2Int> a, List<Vector2Int> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Distância da borda da abertura até a faixa e largura da faixa, tiradas da faixa configurada em relação à
        /// entrada (que tem 1 célula de cada lado do X dela). Para a entrada, a faixa da direita (ou da esquerda) é
        /// exatamente a configurada.
        /// </summary>
        void GetDumpOffsets(out int gap, out int length)
        {
            int entrance = _layout.EntranceX;
            int near, far;
            if (_layout.DumpMinX > entrance)
            {
                near = _layout.DumpMinX - entrance;
                far = _layout.DumpMaxX - entrance;
            }
            else if (_layout.DumpMaxX < entrance)
            {
                near = entrance - _layout.DumpMaxX;
                far = entrance - _layout.DumpMinX;
            }
            else
            {
                // Faixa configurada por cima da entrada: afasta do buraco e mantém o alcance.
                near = 3;
                far = Mathf.Max(near, Mathf.Max(_layout.DumpMaxX - entrance, entrance - _layout.DumpMinX));
            }
            gap = Mathf.Max(1, near - 1);
            length = far - near;
        }

        void AddDumpSide(int group, int minX, int maxX, RectInt area)
        {
            // Fora da moldura do mapa (a borda da área ativa é Bedrock no gerador).
            minX = Mathf.Max(minX, area.xMin + 1);
            maxX = Mathf.Min(maxX, area.xMax - 2);
            if (minX > maxX)
                return;

            FlowField field;
            if (_fieldPool.Count > 0)
            {
                field = _fieldPool[_fieldPool.Count - 1];
                _fieldPool.RemoveAt(_fieldPool.Count - 1);
                field.Invalidate();
            }
            else
            {
                field = new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            }
            _dumpSides.Add(new DumpSide(group, minX, maxX, field));
            _dumpStrips.Add(new Vector2Int(minX, maxX));
        }

        /// <summary>
        /// Terra já empilhada na faixa (células sólidas contíguas acima do solo original de cada coluna) mais os pixels
        /// a caminho dela. Mede o monte real, então conta também o que escorreu ou foi compactado pela chuva.
        /// </summary>
        int DumpLoad(DumpSide side)
        {
            int load = side.InFlight;
            for (int x = side.MinX; x <= side.MaxX; x++)
            for (int y = _grid.GetSurfaceY(x) + 1; y < _grid.Height && _grid.IsSolid(x, y); y++)
                load++;
            return load;
        }

        /// <summary>
        /// Escolhe a abertura mais perto (pelo caminho) e, nela, o lado com menos terra; empate é sorteado. Lados
        /// alcançáveis vêm antes; sem nenhum alcançável, a formiga fica com um lado qualquer e vaga até abrir caminho.
        /// </summary>
        void AssignDumpSide(Ant ant)
        {
            ReleaseDumpSide(ant);
            if (_dumpSides.Count == 0)
                return;

            int group = _dumpSides[0].Group;
            int bestDistance = int.MaxValue;
            foreach (DumpSide side in _dumpSides)
            {
                int d = side.Field.Get(ant.X, ant.Y);
                if (d != FlowField.Unreachable && d < bestDistance)
                {
                    bestDistance = d;
                    group = side.Group;
                }
            }

            int chosen = -1, bestLoad = int.MaxValue, ties = 0;
            bool chosenReachable = false;
            for (int i = 0; i < _dumpSides.Count; i++)
            {
                DumpSide side = _dumpSides[i];
                if (side.Group != group)
                    continue;
                bool reachable = side.Field.Get(ant.X, ant.Y) != FlowField.Unreachable;
                if (chosenReachable && !reachable)
                    continue;
                int load = DumpLoad(side);
                if (reachable && !chosenReachable || load < bestLoad)
                {
                    chosen = i;
                    bestLoad = load;
                    ties = 1;
                    chosenReachable = reachable;
                }
                else if (load == bestLoad && _rng.Next(++ties) == 0)
                {
                    chosen = i;
                }
            }

            DumpSide target = _dumpSides[chosen];
            ant.DumpSide = chosen;
            ant.DumpTargetX = _rng.Next(target.MinX, target.MaxX + 1);
            target.InFlight++;
        }

        void ReleaseDumpSide(Ant ant)
        {
            if (ant.DumpSide >= 0)
                _dumpSides[ant.DumpSide].InFlight--;
            ant.DumpSide = -1;
        }

        // ---------------------------------------------------------------- rainha

        /// <summary>
        /// Acha os grupos conectados (8 direções) de células pintadas como sala da rainha e caminháveis, e fica com o
        /// maior; no empate, com o que já tem a rainha, senão o primeiro achado. Monta o campo até essa sala.
        /// </summary>
        void UpdateQueenRoom()
        {
            _roomCells.Clear();
            _queenLabel = 0;
            if (_grid.QueenRoomCount > 0)
            {
                int w = _grid.Width;
                int queenIndex = _grid.ToIndex(Queen.X, Queen.Y);
                int firstLabel = _labelCounter + 1;
                bool bestHasQueen = false;
                RectInt area = _grid.Active;
                int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
                int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
                for (int cy = cyMin; cy <= cyMax; cy++)
                for (int cx = cxMin; cx <= cxMax; cx++)
                {
                    if (_grid.GetChunkQueenRoomCount(cx, cy) == 0)
                        continue;
                    int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                    int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                    int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                    int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                    for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        int i = y * w + x;
                        if (_roomLabel[i] >= firstLabel || !IsRoomCell(x, y, i))
                            continue;
                        int label = ++_labelCounter;
                        int size = FloodRoom(i, label, firstLabel, queenIndex, out bool hasQueen);
                        if (size > _roomCells.Count || size == _roomCells.Count && hasQueen && !bestHasQueen)
                        {
                            _queenLabel = label;
                            bestHasQueen = hasQueen;
                            _roomCells.Clear();
                            for (int n = 0; n < size; n++)
                                _roomCells.Add(_floodQueue[n]);
                        }
                    }
                }
            }
        }

        bool IsRoomCell(int x, int y, int index) => _grid.IsQueenRoom(x, y) && _walkability.IsWalkableAt(index);

        /// <summary>Rotula o grupo de células de sala a partir de <paramref name="start"/>; as células ficam em _floodQueue[0..tamanho).</summary>
        int FloodRoom(int start, int label, int firstLabel, int queenIndex, out bool hasQueen)
        {
            int w = _grid.Width;
            int head = 0, tail = 0;
            _roomLabel[start] = label;
            _floodQueue[tail++] = start;
            hasQueen = false;
            while (head < tail)
            {
                int i = _floodQueue[head++];
                hasQueen |= i == queenIndex;
                int x = i % w, y = i / w;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k];
                    int ny = y + AntNavigation.Dy[k];
                    if (!_grid.InBounds(nx, ny))
                        continue;
                    int ni = ny * w + nx;
                    if (_roomLabel[ni] >= firstLabel || !IsRoomCell(nx, ny, ni))
                        continue;
                    _roomLabel[ni] = label;
                    _floodQueue[tail++] = ni;
                }
            }
            return tail;
        }

        /// <summary>Origem dos campos de casa: a sala inteira quando a rainha está nela (estável), senão a célula dela.</summary>
        int HomeAnchor() => QueenInRoom ? -_queenLabel : _grid.ToIndex(Queen.X, Queen.Y);

        /// <summary>
        /// Campo de casa (por qualquer célula caminhável, para voltar de fora) e região do formigueiro ligada à rainha
        /// (só túnel e sem passar por marcas de construção: as ociosas não ficam em cima da obra nem atrás dela), os
        /// dois a partir da sala da rainha, ou da célula dela fora da sala.
        /// </summary>
        void BuildHomeField()
        {
            _sources.Clear();
            if (QueenInRoom)
            {
                _sources.AddRange(_roomCells);
            }
            else if (_walkability.IsWalkable(Queen.X, Queen.Y))
            {
                _sources.Add(_grid.ToIndex(Queen.X, Queen.Y));
            }
            else
            {
                for (int k = 0; k < 8; k++)
                {
                    int x = Queen.X + AntNavigation.Dx[k];
                    int y = Queen.Y + AntNavigation.Dy[k];
                    if (_walkability.IsWalkable(x, y))
                        _sources.Add(_grid.ToIndex(x, y));
                }
            }
            UpdateField(_homeField, _sources);
            // O céu não entra na região do formigueiro: ela só muda com o túnel, as marcas ou a rainha.
            if (_pendingUnderground || _pendingMarks || _pendingQueenMoved)
                BuildColonyRegion();
            _homeAnchor = HomeAnchor();
        }

        void BuildColonyRegion()
        {
            int w = _grid.Width;
            for (int n = _sources.Count - 1; n >= 0; n--)
            {
                int i = _sources[n];
                if (i / w > _grid.GetSurfaceY(i % w) || _grid.IsBuildMarked(i % w, i / w))
                    _sources.RemoveAt(n);
            }
            _colonyField.Build(_walkability, _sources, anthillOnly: true);
            RecountUsableSpace();
        }

        /// <summary>
        /// Sem sala, a rainha fica parada. Com sala, vai até ela pelo caminho e, dentro, anda só pelas células da sala,
        /// com paradas de vez em quando. Se a sala estiver fora de alcance, espera onde está.
        /// </summary>
        void UpdateQueen(float deltaTime)
        {
            Ant queen = Queen;
            if (UpdateSupport(queen, deltaTime))
                return;
            queen.Cooldown -= deltaTime;
            if (queen.Cooldown > 0f)
                return;

            if (QueenInRoom)
            {
                if (_rng.NextDouble() < _settings.QueenRestChance)
                    queen.Cooldown = (float)_rng.NextDouble() * _settings.QueenRestSeconds;
                else
                    Wander(queen, roomLabel: _queenLabel);
                return;
            }
            int d = _queenRoomField.Get(queen.X, queen.Y);
            if (d != FlowField.Unreachable && d > 0 && StepDownhill(queen, _queenRoomField))
                return;
            queen.Cooldown = QueenWaitSeconds;
        }

        // ---------------------------------------------------------------- comportamento

        /// <summary>
        /// Soterrada, sobe até achar espaço; sem apoio, se agarra ou cai. Devolve true se o passo foi gasto nisso.
        /// </summary>
        bool UpdateSupport(Ant ant, float deltaTime)
        {
            // Material granular pode cair por cima da formiga: ela sobe até achar espaço.
            // (Planta marcada é sólida mas caminhável: a formiga fica dentro dela.)
            if (_grid.IsSolid(ant.X, ant.Y) && !AntNavigation.IsWalkable(_grid, ant.X, ant.Y))
                PushOutOfSolid(ant);

            // Perdeu o apoio (ex.: cavou o próprio piso): agarra a superfície vizinha se houver;
            // só cai, uma célula por passo, quando não há nada ao alcance.
            if (AntNavigation.IsWalkable(_grid, ant.X, ant.Y))
                return false;
            if (TryCling(ant))
                return true;
            if (!_grid.IsSolid(ant.X, ant.Y - 1))
            {
                BeginMove(ant, deltaTime);
                ant.Y--;
            }
            return true;
        }

        void UpdateAnt(Ant ant, float deltaTime)
        {
            if (UpdateSupport(ant, deltaTime))
                return;

            ant.Cooldown -= deltaTime;
            if (ant.Cooldown > 0f)
                return;

            switch (ant.State)
            {
                case AntState.Carrying:
                    if (CellMaterials.IsPlant(ant.Carried))
                        UpdateStoring(ant);
                    else
                        UpdateCarrying(ant);
                    break;
                case AntState.Fetching:
                    UpdateFetching(ant);
                    break;
                case AntState.Delivering:
                    UpdateDelivering(ant);
                    break;
                case AntState.FetchingEgg:
                    UpdateFetchingEgg(ant);
                    break;
                case AntState.CarryingEgg:
                    UpdateCarryingEgg(ant);
                    break;
                case AntState.Tending:
                    UpdateTending(ant);
                    break;
                case AntState.MovingBrood:
                    UpdateMovingBrood(ant);
                    break;
                case AntState.Gardening:
                    UpdateGardening(ant);
                    break;
                case AntState.MovingNutrient:
                    UpdateMovingNutrient(ant);
                    break;
                case AntState.Digging:
                    FinishDig(ant);
                    break;
                case AntState.Gathering:
                    FinishGather(ant);
                    break;
                case AntState.Hauling:
                    UpdateHauling(ant);
                    break;
                case AntState.Placing:
                    FinishPlace(ant);
                    break;
                default:
                    UpdateIdle(ant);
                    break;
            }
        }

        /// <summary>Até onde (em passos do campo de escavação) uma formiga com a mão não cheia vai buscar mais um pixel antes de entregar.</summary>
        const int LoadedSeekRange = 4;

        void UpdateIdle(Ant ant)
        {
            // Mão com pixels e sem alvo ao alcance: procura mais um perto; senão entrega o que tem.
            if (ant.CarriedCount > 0)
            {
                UpdateLoadedIdle(ant);
                return;
            }

            // A cortadeira só corta: quem busca nutriente, recolhe pixel solto, cava e constrói são as outras.
            if (AntRoles.CutsFirst(ant.Role))
            {
                UpdateCutterIdle(ant);
                return;
            }

            // Ovo vem antes de tudo; a escavadora não leva ovo nem nutriente, ela prioriza as obras.
            // A especialista (cuidadora, jardineira) vai à região antes de tudo; a comum, logo depois do ovo e antes das obras.
            bool specialist = ant.Role == AntRole.Nurse || ant.Role == AntRole.Gardener;
            if (specialist && TryStartCare(ant))
                return;

            if (AntRoles.CarriesEggs(ant.Role) && TryStartEggFetch(ant))
                return;

            if (!specialist && TryStartCare(ant))
                return;

            // A cuidadora vive no berçário: sem cria para cuidar nem ovo para levar, vaga pelo formigueiro.
            if (ant.Role == AntRole.Nurse)
            {
                WanderHome(ant);
                return;
            }

            if (ant.Role == AntRole.Gardener)
            {
                if (_grid.ReadyNutrientCount > 0 && TryStartFetch(ant))
                    return;
                WanderHome(ant);
                return;
            }

            if (AntRoles.FetchesNutrients(ant.Role) && _grid.ReadyNutrientCount > 0 && TryStartFetch(ant))
                return;

            if (_grid.MarkedCount > 0)
            {
                int d = _digField.Get(ant.X, ant.Y);
                if (d == 0 && TryStartDig(ant))
                    return;
                if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, _digField))
                    return;
                // Rocha marcada: só a escavadora com E2, depois da terra.
                if (RockOpen && Tree.CanBreakRock(ant.Role))
                {
                    int rd = _rockField.Get(ant.X, ant.Y);
                    if (rd == 0 && TryStartDig(ant, rock: true))
                        return;
                    if (rd != FlowField.Unreachable && rd > 0 && StepDownhill(ant, _rockField))
                        return;
                }
            }

            if (_hasSite && TryBuildWork(ant))
                return;

            // Sem mais nada a fazer, a que também corta (comum, carregadora) vai à planta-alvo.
            if (AntRoles.Harvests(ant.Role) && TryCutWork(ant))
                return;

            WanderHome(ant);
        }

        void UpdateLoadedIdle(Ant ant)
        {
            int d = _digField.Get(ant.X, ant.Y);
            if (ant.CarriedCount < LoadCapacity(ant) && ant.Carried != CellMaterial.Rock && d != FlowField.Unreachable && d <= LoadedSeekRange)
            {
                if (d == 0 && TryStartDig(ant))
                    return;
                if (d > 0 && StepDownhill(ant, _digField))
                    return;
            }
            StartDelivery(ant);
        }

        /// <summary>Sem trabalho: vaga por toda a região do formigueiro ligada à rainha; de fora dela, volta pelo caminho.</summary>
        void WanderHome(Ant ant)
        {
            if (_colonyField.Get(ant.X, ant.Y) != FlowField.Unreachable)
            {
                Wander(ant, _colonyField);
                return;
            }
            if (_homeField.Get(ant.X, ant.Y) != FlowField.Unreachable && StepDownhill(ant, _homeField))
                return;
            Wander(ant);
        }

        /// <summary>
        /// Cortadeira ociosa: corta a planta-alvo. Sem alvo, ou com a sala de nutrientes cheia, volta e vaga pelo
        /// formigueiro até abrir espaço; longe da planta e sem caminho, vaga onde está.
        /// </summary>
        void UpdateCutterIdle(Ant ant)
        {
            if (_gardenTarget < 0 || _harvestPaused)
            {
                WanderHome(ant);
                return;
            }
            if (!TryCutWork(ant))
                Wander(ant);
        }

        /// <summary>
        /// Vai à linha de cima da planta-alvo pelo campo de corte e corta. Se todos os pixels da linha estão travados por
        /// outras, espera ali por perto. <c>false</c> = não há o que cortar para esta formiga (sem alvo, sala cheia ou sem caminho).
        /// </summary>
        bool TryCutWork(Ant ant)
        {
            if (_gardenTarget < 0 || _harvestPaused)
                return false;
            int d = _gardenField.Get(ant.X, ant.Y);
            if (d == 0 && TryStartDig(ant, cutting: true))
                return true;
            if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, _gardenField))
                return true;
            if (d != FlowField.Unreachable || IsNearPlant(ant))
            {
                Hold(ant);
                return true;
            }
            return false;
        }

        /// <summary>A formiga está na faixa de colunas da planta-alvo (espera ali em vez de ir embora).</summary>
        bool IsNearPlant(Ant ant) => ant.X >= _gardenMinX - 2 && ant.X <= _gardenMaxX + 2;

        /// <param name="cutting">Corta a planta em pé (alvo de colheita) em vez de pegar o que está marcado.</param>
        /// <param name="rock">Quebra uma rocha marcada (só escavadora com E2).</param>
        bool TryStartDig(Ant ant, bool cutting = false, bool rock = false)
        {
            // Quem corta só começa se a sala de nutrientes é alcançável de onde está.
            if (cutting && _storeField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            int count = 0;
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                bool target = cutting ? IsPlantTarget(nx, ny)
                    : rock ? IsRockTarget(nx, ny)
                    : IsWorkTarget(nx, ny, ant.X, ant.Y) && FitsLoad(ant, nx, ny);
                if (target)
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
            ClaimTarget(ant);
            // A taxa de quebra da rocha (4 células/s) não muda com a velocidade de escavar.
            ant.Cooldown = rock
                ? _settings.RockBreakSeconds * _settings.DigTimeMultiplier
                : CellMaterials.DigSeconds(_grid.Get(ant.DigX, ant.DigY)) * _settings.DigTimeMultiplier * WorkTimeFactor(ant);
            return true;
        }

        /// <summary>Com a mão cheia pela metade, só vale o que é do mesmo tipo do que já se leva (terra com terra, planta com planta).</summary>
        bool FitsLoad(Ant ant, int x, int y)
        {
            if (ant.CarriedCount == 0)
                return true;
            return CellMaterials.IsPlant(ant.Carried) == CellMaterials.IsPlantDebris(_grid.Get(x, y));
        }

        /// <summary>Quantos pixels a formiga leva por viagem do que tem na mão (planta, terra ou rocha).</summary>
        int LoadCapacity(Ant ant)
        {
            if (CellMaterials.IsPlant(ant.Carried))
                return Tree.MaterialCapacity(ant.Role);
            return ant.Carried == CellMaterial.Rock ? 1 : Tree.SoilCapacity(ant.Role);
        }

        /// <summary>Segue com o que tem na mão: planta vai à sala de nutrientes (UpdateStoring); o resto, ao despejo.</summary>
        void StartDelivery(Ant ant)
        {
            ant.State = AntState.Carrying;
            if (!CellMaterials.IsPlant(ant.Carried))
                AssignDumpSide(ant);
        }

        void FinishDig(Ant ant)
        {
            ant.State = AntState.Idle;
            bool adjacent = Mathf.Abs(ant.DigX - ant.X) <= 1 && Mathf.Abs(ant.DigY - ant.Y) <= 1;
            CellMaterial material = _grid.Get(ant.DigX, ant.DigY);
            // Outra formiga pode ter levado o pixel, ou ele caiu/foi desmarcado durante a escavação. A planta em pé não
            // tem marca (o alvo é escolhido pela colônia): basta ainda ser um pixel cortável.
            bool plant = CellMaterials.IsPlant(material);
            bool debris = CellMaterials.IsPlantDebris(material);
            bool rock = material == CellMaterial.Rock && Tree.CanBreakRock(ant.Role);
            if (!adjacent || !(plant ? VegetationSimulation.CanHarvest(_grid, ant.DigX, ant.DigY)
                                     : _grid.IsMarked(ant.DigX, ant.DigY) && (debris || rock || CellMaterials.IsDiggable(material))))
            {
                // O alvo sumiu no meio da escavação: o que já está na mão segue para a entrega.
                if (ant.CarriedCount > 0)
                    StartDelivery(ant);
                return;
            }

            if (plant)
            {
                // A formiga só solta o pixel: ele ganha a física granular, cai e mantém a marca de colheita. Passa por
                // Empty porque trocar planta por material não-planta guardaria a planta atrás da célula (PushBehindAt).
                byte shade = _grid.GetShade(ant.DigX, ant.DigY);
                _grid.Set(ant.DigX, ant.DigY, CellMaterial.Empty, 0);
                _grid.Set(ant.DigX, ant.DigY, CellMaterials.ToDebris(material), shade);
                _grid.SetMarked(ant.DigX, ant.DigY, true);
                VegetationSimulation.DropDisconnected(_grid, ant.DigX, ant.DigY);
                return;
            }

            // Pixel solto de planta volta a ser planta na mão e vai para a sala de nutrientes (UpdateStoring).
            ant.PickUp(debris ? CellMaterials.FromDebris(material) : material, _grid.GetShade(ant.DigX, ant.DigY));
            _grid.Set(ant.DigX, ant.DigY, CellMaterial.Empty, 0);
            // Mão não cheia e mais um pixel do mesmo tipo ao alcance: continua antes de entregar.
            if (ant.CarriedCount >= LoadCapacity(ant))
            {
                StartDelivery(ant);
                return;
            }
            // Sem alvo encostado, fica ociosa com a carga: UpdateLoadedIdle procura um perto ou entrega.
            TryStartDig(ant);
        }

        /// <summary>Sem terra na mão: vai até a pilha de fora buscar um pixel (só se ainda faltar quem construa).</summary>
        bool TryBuildWork(Ant ant)
        {
            // Quem pega terra entra na fila mesmo sem ponto livre agora: espera perto da obra em vez de voltar ocioso.
            if (_activeBuilders >= _grid.BuildMarkedCount)
                return false;
            int d = _pileField.Get(ant.X, ant.Y);
            if (d == FlowField.Unreachable)
                return false;
            if (d == 0)
                return TryStartGather(ant);
            return StepDownhill(ant, _pileField);
        }

        bool TryStartGather(Ant ant, bool continuing = false)
        {
            int count = 0;
            for (int k = 0; k < 8; k++)
            {
                // Tirar o pixel de baixo desmonta o próprio apoio: só se não houver outro.
                if (k != 3 && IsPileCell(ant.X + AntNavigation.Dx[k], ant.Y + AntNavigation.Dy[k]))
                    _candidates[count++] = k;
            }
            if (count == 0 && IsPileCell(ant.X, ant.Y - 1))
                _candidates[count++] = 3;
            if (count == 0)
                return false;

            int pick = _candidates[_rng.Next(count)];
            ant.DigX = ant.X + AntNavigation.Dx[pick];
            ant.DigY = ant.Y + AntNavigation.Dy[pick];
            if (ant.DigX != ant.X)
                ant.FacingX = ant.DigX > ant.X ? 1 : -1;
            ant.State = AntState.Gathering;
            ant.Cooldown = CellMaterials.DigSeconds(_grid.Get(ant.DigX, ant.DigY)) * _settings.DigTimeMultiplier * WorkTimeFactor(ant);
            if (!continuing)
                _activeBuilders++;
            return true;
        }

        void FinishGather(Ant ant)
        {
            ant.State = AntState.Idle;
            bool adjacent = Mathf.Abs(ant.DigX - ant.X) <= 1 && Mathf.Abs(ant.DigY - ant.Y) <= 1;
            if (!adjacent || !IsPileCell(ant.DigX, ant.DigY))
            {
                if (ant.CarriedCount > 0)
                    ant.State = AntState.Hauling;
                return;
            }

            ant.PickUp(_grid.Get(ant.DigX, ant.DigY), _grid.GetShade(ant.DigX, ant.DigY));
            ant.ReturningSoil = false;
            _grid.Set(ant.DigX, ant.DigY, CellMaterial.Empty, 0);
            // Mão não cheia e mais terra solta ao alcance: pega mais antes de ir à obra.
            if (ant.CarriedCount < Tree.SoilCapacity(ant.Role) && TryStartGather(ant, continuing: true))
                return;
            ant.State = AntState.Hauling;
        }

        void UpdateHauling(Ant ant)
        {
            // Primeiro, um ponto livre para encaixar; sem nenhum agora, espera na fila perto da obra.
            // Só devolve a terra ao despejo se não restar nenhum ponto de obra alcançável.
            if (_hasSite)
            {
                int d = _hasBuildWork ? _buildField.Get(ant.X, ant.Y) : FlowField.Unreachable;
                if (d == 0)
                {
                    if (!TryStartPlace(ant))
                        Hold(ant); // o ponto foi tomado por outra formiga: o próximo recálculo manda esta para outro
                    return;
                }
                if (d != FlowField.Unreachable && StepDownhill(ant, _buildField))
                    return;

                int s = _siteField.Get(ant.X, ant.Y);
                if (s == 0)
                {
                    Hold(ant);
                    return;
                }
                if (s != FlowField.Unreachable && StepDownhill(ant, _siteField))
                    return;
            }

            ant.ReturningSoil = true;
            ant.State = AntState.Carrying;
            AssignDumpSide(ant);
        }

        void Hold(Ant ant) => ant.Cooldown = 0.2f;

        bool TryStartPlace(Ant ant)
        {
            int count = 0;
            int antExit = _exitField.Get(ant.X, ant.Y);
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (IsFillable(nx, ny, true) && (IsOutdoor(nx, ny) || antExit < _exitField.Get(nx, ny)) && !AntAt(nx, ny))
                    _candidates[count++] = k;
            }
            if (count == 0)
                return false;

            int pick = _candidates[_rng.Next(count)];
            ant.DigX = ant.X + AntNavigation.Dx[pick];
            ant.DigY = ant.Y + AntNavigation.Dy[pick];
            if (ant.DigX != ant.X)
                ant.FacingX = ant.DigX > ant.X ? 1 : -1;
            ant.State = AntState.Placing;
            ant.Cooldown = _settings.DropSeconds;
            // Reserva o ponto e pede novo cálculo, para as outras formigas buscarem outro.
            _reserved.Add(_grid.ToIndex(ant.DigX, ant.DigY));
            _builtMarkVersion = -1;
            return true;
        }

        void FinishPlace(Ant ant)
        {
            // Se o alvo mudou no meio do caminho, continua com a terra na mão e procura outro.
            ant.State = AntState.Hauling;
            bool adjacent = Mathf.Abs(ant.DigX - ant.X) <= 1 && Mathf.Abs(ant.DigY - ant.Y) <= 1;
            if (!adjacent || !IsFillable(ant.DigX, ant.DigY, false) || AntAt(ant.DigX, ant.DigY))
                return;

            // Terra solta voltaria a escorrer; encaixada, fica onde foi posta (e continua escavável).
            _grid.Set(ant.DigX, ant.DigY, CellMaterials.ToPacked(ant.Carried), ant.CarriedShade);
            ant.DropTop();
            // Sobrou terra na mão: segue para o próximo ponto da obra.
            ant.State = ant.CarriedCount > 0 ? AntState.Hauling : AntState.Idle;
        }

        void UpdateCarrying(Ant ant)
        {
            if (ant.DumpSide < 0)
                AssignDumpSide(ant);
            if (ant.DumpSide < 0)
            {
                Wander(ant);
                return;
            }
            FlowField field = _dumpSides[ant.DumpSide].Field;
            int d = field.Get(ant.X, ant.Y);
            if (d == FlowField.Unreachable && TrySwitchDumpSide(ant))
            {
                field = _dumpSides[ant.DumpSide].Field;
                d = field.Get(ant.X, ant.Y);
            }
            if (d == FlowField.Unreachable)
            {
                Wander(ant);
                return;
            }
            if (d > 0)
            {
                if (!StepDownhill(ant, field))
                    Wander(ant);
                return;
            }

            // Dentro da faixa de despejo: anda até o X sorteado para espalhar a pilha.
            if (ant.X != ant.DumpTargetX && StepTowardsDumpTarget(ant))
                return;
            if (!TryDrop(ant))
                Wander(ant);
        }

        /// <summary>O lado escolhido ficou inalcançável (ex.: túnel desabou): escolhe de novo entre os alcançáveis, se houver.</summary>
        bool TrySwitchDumpSide(Ant ant)
        {
            bool anyReachable = false;
            foreach (DumpSide side in _dumpSides)
                anyReachable |= side.Field.Get(ant.X, ant.Y) != FlowField.Unreachable;
            if (!anyReachable)
                return false;
            AssignDumpSide(ant);
            return true;
        }

        bool StepTowardsDumpTarget(Ant ant)
        {
            FlowField field = _dumpSides[ant.DumpSide].Field;
            int bestK = -1;
            int bestDistance = Mathf.Abs(ant.X - ant.DumpTargetX);
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (field.Get(nx, ny) != 0 || !AntNavigation.IsWalkable(_grid, nx, ny))
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
                ant.DropTop();
                ant.Cooldown = _settings.DropSeconds;
                // Terra que só voltou ao despejo (construção cancelada) não é entrega nova.
                if (!ant.ReturningSoil)
                {
                    StoredPixels++;
                    TotalDelivered++;
                }
                // Ainda tem carga: continua despejando, um pixel por vez, na mesma faixa.
                if (ant.CarriedCount > 0)
                    return true;
                ReleaseDumpSide(ant);
                ant.State = AntState.Idle;
                ant.ReturningSoil = false;
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

        /// <summary>
        /// Anda a esmo, preferindo seguir em frente. <paramref name="region"/> limita às células alcançadas por esse
        /// campo e <paramref name="roomLabel"/> às da sala com esse rótulo.
        /// </summary>
        void Wander(Ant ant, FlowField region = null, int roomLabel = 0)
        {
            int forward = 0;
            int any = 0;
            Span<int> forwardMoves = stackalloc int[8];
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (!AntNavigation.IsWalkable(_grid, nx, ny))
                    continue;
                if (region != null && region.Get(nx, ny) == FlowField.Unreachable)
                    continue;
                if (roomLabel != 0 && _roomLabel[_grid.ToIndex(nx, ny)] != roomLabel)
                    continue;
                _candidates[any++] = k;
                if (AntNavigation.Dx[k] == ant.FacingX)
                    forwardMoves[forward++] = k;
            }

            if (any == 0)
            {
                // Presa fora da região (campo ainda não recalculado): anda sem o limite. A rainha nunca sai da sala.
                if (region != null)
                    Wander(ant);
                else
                    ant.Cooldown = 1f / SpeedOf(ant);
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
            float step = (dx != 0 && dy != 0 ? 1.4142f : 1f) / SpeedOf(ant);
            BeginMove(ant, step);
            ant.X += dx;
            ant.Y += dy;
            if (dx != 0)
                ant.FacingX = dx;
            ant.Cooldown += step;
            if (ant.Cooldown < 0f)
                ant.Cooldown = 0f;
        }

        float SpeedOf(Ant ant) =>
            ReferenceEquals(ant, Queen) ? _settings.QueenSpeed : _settings.AntSpeed * AntRoles.SpeedFactor(ant.Role) * Tree.SpeedFactor(ant.Role);

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

        sealed class DumpSide
        {
            /// <summary>Abertura a que a faixa pertence (os dois lados de uma abertura têm o mesmo grupo).</summary>
            public readonly int Group;
            public readonly int MinX;
            public readonly int MaxX;
            public readonly FlowField Field;
            /// <summary>Formigas carregando um pixel para esta faixa.</summary>
            public int InFlight;

            public DumpSide(int group, int minX, int maxX, FlowField field)
            {
                Group = group;
                MinX = minX;
                MaxX = maxX;
                Field = field;
            }
        }
    }
}
