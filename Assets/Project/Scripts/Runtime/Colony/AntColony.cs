using System;
using System.Collections.Generic;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Diagnostics;
using Fabula.Incrementant.Juice;
using Fabula.Incrementant.Upgrades;
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
    /// Rainha (<see cref="Queen"/>): o dobro do tamanho no desenho; anda como uma operária. Ociosa, fica na sala da rainha
    /// pintada pelo jogador (a maior, se houver várias); sem sala, vaga pelo formigueiro. As formigas ociosas vagam por
    /// toda a região do formigueiro ligada a ela.
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
        readonly int _seed;
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
        int _openingsLimit;
        readonly FlowField _homeField;
        // Construção: distância até a saída (superfície), até a terra solta lá fora e até onde dá para encaixar um pixel.
        readonly FlowField _exitField;
        readonly FlowField _pileField;
        readonly FlowField _buildField;
        readonly List<int> _buildSources = new List<int>();
        readonly FlowField _siteField;
        readonly List<int> _siteSources = new List<int>();
        readonly List<int> _reserved = new List<int>();
        // Profundidade de cada marca de construção para a ordem da obra (ver OrderDepth) e lista de limpeza.
        readonly Dictionary<int, int> _buildDepth = new Dictionary<int, int>();
        readonly List<int> _buildDepthStale = new List<int>();
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
        // Sala da rainha da rodada anterior, para o render redesenhar só os blocos onde ela mudou.
        readonly List<int> _previousRoomCells = new List<int>();
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

        // Nutrientes: campo até os canteiros que esperam planta (ver AntColony.Cultivation) e campo até os nutrientes prontos.
        readonly FlowField _storeField;
        readonly FlowField _nutrientField;

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
        // Pedaços de planta soltos (folha caída etc.) que alguma formiga já reservou ou está pegando: ocupam a vaga da planta
        // antes de chegar à mão, para a vaga de um pedaço não chamar todas as ociosas (relato de 2026-10-19).
        int _debrisClaims;
        // Formigas que atendem o pedido de planta sem tê-la na mão: indo cortar, esperando no pé ou cortando (Ant.Errand).
        int _cutters;
        int _fetchers;
        int _builtNutrientVersion = -1;
        (bool, bool, bool) _builtHarvestOpen;

        int _builtStructureVersion = -1;
        int _builtMarkVersion = -1;
        float _fieldTimer;

        public IReadOnlyList<Ant> Ants => _ants;

        /// <summary>Há formiga (ou a rainha) na célula. Varre a lista: para consultas raras, como a folha que volta a cobrir o chão.</summary>
        public bool IsAntAt(int x, int y)
        {
            if (Queen != null && Queen.X == x && Queen.Y == y)
                return true;
            for (int i = 0; i < _ants.Count; i++)
                if (_ants[i].X == x && _ants[i].Y == y)
                    return true;
            return false;
        }

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

        /// <summary>Passos de formiga lentos (painel F11); <see cref="_stepMark"/> marca onde da decisão da ociosa o passo estava.</summary>
        public SlowStepLog SlowSteps { get; } = new SlowStepLog();
        int _stepMark;

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

        /// <summary>Pixels de planta colhidos e plantados nos canteiros da sala de nutrientes desde o começo do mapa.</summary>
        public int HarvestedPixels { get; private set; }

        /// <summary>Segundos que um canteiro regado leva para brotar em nutrientes (ver <see cref="ColonySettings"/>).</summary>
        public float NutrientCureSeconds => _settings.NutrientCureSeconds;

        /// <summary>Há canteiro esperando mais um pixel de planta (contando os que já estão sendo colhidos ou levados).</summary>
        public bool CanHarvest => PlantSupplyDemand - _plantsInFlight - PlantsWaiting > 0;

        /// <summary>
        /// Os cortadores pararam porque nenhum canteiro espera planta e voltaram ao formigueiro. Só voltam a cortar quando
        /// mais da metade dos canteiros espera planta (histerese, para não alternar a cada canteiro que brota).
        /// </summary>
        public bool HarvestPaused => _harvestPaused;
        bool _harvestPaused;

        /// <summary>
        /// Vaga para mais um pixel de planta solto no chão (ele mesmo já está contado em <c>PlantDebrisCount</c>), descontados
        /// os que outras formigas já reservaram ou estão pegando (<see cref="_debrisClaims"/>).
        /// </summary>
        bool CanStoreDebris => DebrisRoom > 0;

        /// <summary>Pixels de planta soltos ao alcance das formigas, recontados a cada rodada de campos (<see cref="CountReachableDebris"/>).</summary>
        int _reachableDebris;

        /// <summary>Vaga da planta menos o que está na mão e o que já foi reservado no chão; negativa = reservou-se demais.</summary>
        int DebrisRoom => PlantSupplyDemand - _plantsInFlight - _debrisClaims;

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
        public int AdultCapacity => Mathf.Min(_settings.MaxAnts, (int)(_usableCells / Mathf.Max(1f, _settings.CellsPerAdultSlot * Effects.AdultSlotCellsFactor)));

        /// <summary>Vagas de cria: células vazias e alcançáveis do berçário dividido por <see cref="ColonySettings.CellsPerNurserySlot"/>.</summary>
        public int NurserySlots =>
            (int)(_nurseryCells / Mathf.Max(1f, _settings.CellsPerNurserySlot * Effects.NurseryCellsFactor) * Effects.NurseryCapacityFactor);

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

        /// <summary>Níveis das galerias e câmaras do catálogo de melhorias (documento 39) que já têm sistema; vão no save.</summary>
        public UpgradeLevels Galleries { get; } = new UpgradeLevels();
        /// <summary>Números derivados dos níveis que os sistemas da colônia consultam (neutros sem compras).</summary>
        public UpgradeEffects Effects { get; } = new UpgradeEffects();
        readonly UpgradeBoard _gate = new UpgradeBoard();

        /// <summary>Limite de área do pincel por ambiente (KI-043): células e salas não conectadas. Só o pincel do jogador consulta.</summary>
        public PaintBudget PaintBudget { get; }

        public AntColony(PixelGrid grid, ColonySettings settings, TerrainLayout layout, int seed)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            PaintBudget = new PaintBudget(_grid, _settings, Effects);
            _layout = layout;
            _rng = new System.Random(seed);
            _seed = seed;
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
            _bedSpotField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _waterBedField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _bedPlantField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _waterField = new FlowField(grid.Width, grid.Height, _fieldScratch);
            _soilPileField = new FlowField(grid.Width, grid.Height, _fieldScratch);
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

        /// <summary>Nova formiga do papel dado, nascida na célula (<paramref name="x"/>, <paramref name="y"/>) em vez de junto da rainha. Usada pela vitrine do menu principal.</summary>
        public Ant SpawnAt(AntRole role, int x, int y)
        {
            Ant ant = Spawn(role);
            ant.X = x;
            ant.Y = y;
            PushOutOfSolid(ant);
            SnapVisual(ant);
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
            EmitSpent(cost);
            Emit(SfxId.Buy);
            return BuyBlock.None;
        }

        /// <summary>
        /// Compra uma câmara ou níveis de uma galeria com sistema (<paramref name="count"/> níveis; 0 = quantos o saldo pagar;
        /// câmara = 1). Confere sistema, teto, anel, condição e requisitos do catálogo e o saldo; sem nenhum nível pago nada é
        /// debitado. Os nós da árvore da rainha continuam em <see cref="TryBuy"/>.
        /// </summary>
        public BuyBlock TryBuyUpgrade(UpgradeDef def, int count, out int bought)
        {
            bought = 0;
            if (def.QueenNode.HasValue || !UpgradeBoard.HasSystem(def))
                return BuyBlock.NotAvailableYet;
            bool gallery = def.Kind == UpgradeKind.Gallery;
            int level = Galleries.Level(def.Id);
            if (gallery ? def.Capped && level >= def.Cap : level > 0 || def.Initial)
                return BuyBlock.Owned;
            _gate.Refresh(Tree, RaidsSurvived, _ants.Count, Galleries);
            if (def.Condition == UpgradeCondition.FirstRaid && RaidsSurvived < 1)
                return BuyBlock.NeedsFirstRaid;
            if (!_gate.Unlockable(def))
                return BuyBlock.MissingRequirement;
            long cost;
            if (gallery)
                cost = count == 0
                    ? UpgradeCatalog.MaxAffordable(def, level, Nutrients, out bought)
                    : UpgradeCatalog.CostOfLevels(def, level, count, out bought);
            else
            {
                cost = def.Cost;
                bought = 1;
            }
            if (bought == 0 || cost > Nutrients)
            {
                bought = 0;
                return BuyBlock.InsufficientNutrients;
            }
            Galleries.Set(def.Id, level + bought);
            Effects.Recompute(Galleries);
            Tree.FarmAvailable = Effects.FarmUnlocked;
            Nutrients -= (int)cost;
            EmitSpent((int)cost);
            Emit(SfxId.Buy);
            return BuyBlock.None;
        }

        /// <summary>Substitui os níveis das galerias e câmaras (carregar save) e reaplica os efeitos.</summary>
        public void RestoreGalleries(string[] ids, int[] levels)
        {
            Galleries.Restore(ids, levels);
            Effects.Recompute(Galleries);
            Tree.FarmAvailable = Effects.FarmUnlocked;
        }

        /// <summary>Custo (nutrientes) de criar uma formiga do papel dado: o alimento por cria do design, estável (não cresce com a colônia).</summary>
        public int CostOf(AntRole role) => Mathf.Max(1, Mathf.RoundToInt(AntRoles.FoodCost(role) * Effects.BroodCostFactor));

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

        BroodContext BuildBroodContext()
        {
            AntRole role = NextAntRole ?? AntRole.Worker;
            _brood.LayIntervalFactor = Tree.LayIntervalFactor * Effects.LayTime;
            _brood.LarvaTimeFactor = Effects.LarvaTime;
            _brood.PupaTimeFactor = Effects.PupaTime;
            _brood.EggsPerLay = Tree.EggsPerLay;
            return new BroodContext
            {
                Mode = QueenMode, Role = role, Cost = CostOf(role), Nutrients = Nutrients, Adults = _ants.Count,
                AdultCapacity = AdultCapacity, NurserySlots = NurserySlots, QueenX = Queen.X, QueenY = Queen.Y,
                QueenOutsideRoom = !QueenInRoom && !Effects.QueenLaysAnywhere,
                RoleProvider = _nextRoleProvider, CostProvider = _costProvider,
                CareCapacity = _nursery.CapacityInside, Boosts = _boosts,
            };
        }

        /// <summary>A célula está sobre a rainha (que ocupa o dobro do tamanho de uma formiga).</summary>
        public bool IsQueenAt(int x, int y) => Mathf.Abs(x - Queen.X) <= 1 && Mathf.Abs(y - Queen.Y) <= 1;

        /// <summary>
        /// Clique na rainha: põe um ovo já, sem esperar o intervalo e ignorando a reserva mínima de nutrientes. Ainda exige
        /// berçário com vaga, capacidade adulta e nutrientes para o custo da cria.
        /// </summary>
        public bool ForceLay()
        {
            if (GameOver)
                return false;
            BroodContext context = BuildBroodContext();
            int spent = _brood.ForceLay(context);
            Nutrients -= spent;
            EmitSpent(spent);
            if (spent > 0)
                Emit(SfxId.EggLaid, Queen.X, Queen.Y);
            return spent > 0;
        }

        void TickBrood(float deltaTime)
        {
            BroodContext context = BuildBroodContext();
            _born.Clear();
            int spent = _brood.Tick(deltaTime, context, _born);
            Nutrients -= spent;
            EmitSpent(spent);
            if (spent > 0)
                Emit(SfxId.EggLaid, Queen.X, Queen.Y);
            foreach (AntRole born in _born)
            {
                Ant newborn = Spawn(born);
                Emit(SfxId.AntBorn, newborn.X, newborn.Y);
                EmitFx(FxId.Hatch, newborn.X, newborn.Y);
            }
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
        /// e a altura (<see cref="NurserySpotHeight"/>) do sprite, então as crias não se sobrepõem. Só vira ponto a célula
        /// em que o sprite inteiro cabe no vazio (<see cref="FitsBrood"/>).
        /// </summary>
        void PickNurserySpots()
        {
            _nurseryFloor.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            _nurseryTaken.Clear();
            foreach (Vector2Int cell in _nurseryFloor)
            {
                if (_nurseryTaken.Contains(cell) || !FitsBrood(cell))
                    continue;
                _nurserySpots.Add(cell);
                for (int dy = -(NurserySpotHeight - 1); dy < NurserySpotHeight; dy++)
                for (int dx = -(NurserySpotSpacing - 1); dx < NurserySpotSpacing; dx++)
                    _nurseryTaken.Add(new Vector2Int(cell.x + dx, cell.y + dy));
            }
        }

        /// <summary>
        /// O sprite da cria (<see cref="NurserySpotSpacing"/> de largura centrado na célula, <see cref="NurserySpotHeight"/>
        /// de altura a partir dela) só pega células vazias. A pintura do berçário aceita parede, e um pixel vazio solto
        /// na parede (buraco de um pixel no chão, quina do teto) é alcançável pela diagonal: sem isto ele virava ponto, a
        /// cria aparecia dentro da parede e as formigas entravam nela para buscá-la (relato de 2026-10-19).
        /// </summary>
        bool FitsBrood(Vector2Int cell)
        {
            int half = NurserySpotSpacing / 2;
            for (int dy = 0; dy < NurserySpotHeight; dy++)
            for (int dx = -half; dx <= half; dx++)
            {
                if (_grid.IsSolid(cell.x + dx, cell.y + dy))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Substitui formigas e economia pelas de um save. A grade já precisa estar restaurada. As formigas voltam sem
        /// faixa de despejo escolhida (quem carrega escolhe de novo no próximo passo) e sem animação em andamento.
        /// </summary>
        public void RestoreState(int storedPixels, int totalDelivered, float simulationTime, IEnumerable<Ant> ants)
        {
            _builtNutrientVersion = -1;
            // A grade foi trocada de uma vez: nenhum campo vale mais.
            _builtMarkVersion = -1;
            _dirtyUnderground = _dirtySurface = _dirtyStructure = _dirtyNearMarks = true;
            foreach (DumpSide side in _dumpSides)
                side.InFlight = 0;
            _ants.Clear();
            SimulationTime = simulationTime;
            Rates.Reset(simulationTime);
            StoredPixels = storedPixels;
            TotalDelivered = totalDelivered;
            foreach (Ant ant in ants)
            {
                // O transporte do ovo não vai para o save (o ovo volta já no berçário): a formiga volta a ficar ociosa.
                if (ant.State == AntState.FetchingEgg || ant.State == AntState.CarryingEgg ||
                    ant.State == AntState.Tending || ant.State == AntState.MovingBrood ||
                    ant.State == AntState.Gardening || ant.State == AntState.MovingNutrient ||
                    ant.State == AntState.Fighting || ant.State == AntState.Dismembering || ant.State == AntState.CarryingWaste ||
                    ant.State == AntState.TendingDew || ant.State == AntState.TakingStored || ant.State == AntState.Rescuing)
                    ant.State = AntState.Idle;
                ant.DumpSide = -1;
                ant.GuardGroup = -1;
                SnapVisual(ant);
                ant.MoveStart = simulationTime;
                _ants.Add(ant);
            }
            _gardenTarget = -1;
            _gardenDirty = true;
            // O ataque em curso não vai para o save; os resíduos voltam a se decompor do começo.
            _termites.Clear();
            _spits.Clear();
            RaidState = RaidState.Calm;
            _raidDirty = true;
            RescanWaste();
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
            if (GameOver)
                return;
            long stageStart = SimProfiler.Now();
            SimulationTime += deltaTime;
            _seekTick++;
            _activeBuilders = 0;
            _plantsInFlight = 0;
            _debrisClaims = 0;
            _cutters = 0;
            _soilCarriers = 0;
            _dewCarriers = 0;
            _waterCarriers = 0;
            _fetchers = 0;
            _harvesters = 0;
            _diggers = 0;
            _dismemberers = 0;
            ResetWasteCounts();
            ResetStorageCounts();
            ResetFarmCounts();
            ClearSectionCounts();
            ResetCare();
            _claimed.Clear();
            _seekOwner.Clear();
            _claimHash = 0;
            _waterClaims.Clear();
            _waterClaimHash = 0;
            _slotClaims.Clear();
            _slotClaimHash = 0;
            _dewSoilClaims.Clear();
            _dewClaimHash = 0;
            _reserved.Clear();
            foreach (Ant ant in _ants)
            {
                CountWork(ant);
                CountSection(ant);
            }
            // A rainha trabalhando (ou sozinha no berçário) reserva e pede como as formigas; fora das seções, não conta nelas.
            if (QueenWorks)
                CountWork(Queen);
            // Os canteiros só brotam com formiga trabalhando na sala; o ritmo da especialista vale só para o que ela toca.
            ApplyGardenBoosts(deltaTime);
            AdvanceBeds(deltaTime);
            UpdateHarvestPause();
            UpdateSectionWanted();
            // Relógio dos ataques, voo e pouso: antes dos campos, que precisam saber quantos cupins estão no chão.
            TickRaids(deltaTime);
            Profiler.Add(ProfileStage.TickPrep, stageStart);
            RefreshFields(deltaTime);
            stageStart = SimProfiler.Now();
            // Os cupins não andam enquanto a rainha e as formigas agem: o índice deles vale até UpdateTermites.
            IndexTermites();
            UpdateQueen(deltaTime);
            Profiler.Add(ProfileStage.Queen, stageStart);
            stageStart = SimProfiler.Now();
            TickBrood(deltaTime);
            Profiler.Add(ProfileStage.Brood, stageStart);
            stageStart = SimProfiler.Now();
            for (int i = 0; i < _ants.Count; i++)
                UpdateAnt(_ants[i], deltaTime);
            _termiteIndex.Invalidate();
            Profiler.Add(ProfileStage.Ants, stageStart);
            stageStart = SimProfiler.Now();
            // Cupins depois das formigas: mordem quem ficou ao alcance, destroem e saem; as formigas mortas em combate saem aqui
            // (formigas só morrem em combate: KI-025, 2026-10-20).
            UpdateTermites(deltaTime);
            HealAnts(deltaTime);
            TickWaste(deltaTime);
            TickDew(deltaTime);
            TickFarm(deltaTime);
            TickWorkshops(deltaTime);
            Profiler.Add(ProfileStage.Termites, stageStart);
            SampleRates();
        }

        /// <summary>Reservas, pedidos e contagens do passo de uma formiga (ou da rainha trabalhando), antes de todas agirem.</summary>
        void CountWork(Ant ant)
        {
            if (ant.State == AntState.Digging)
            {
                ClaimTarget(ant);
                if (CellMaterials.IsPlantDebris(_grid.Get(ant.DigX, ant.DigY)))
                    _debrisClaims++;
            }
            // A reserva vale só para quem ainda está ociosa indo pegar o pixel; se mudou de estado, ela se desfaz.
            if (ant.SeekTarget >= 0)
            {
                if (ant.State == AntState.Idle)
                {
                    ReserveSeek(ant);
                    if (!ant.SeekRock && IsDebrisAt(ant.SeekTarget))
                        _debrisClaims++;
                }
                else if (ant.State == AntState.FetchingWater || ant.State == AntState.CarryingWater)
                    ClaimWater(ant.SeekTarget);
                else if (ant.State == AntState.BringingSoil && ant.DewJob)
                    ClaimDewSoil(ant.SeekTarget);
                else
                    ant.ClearSeek();
            }
            if (AntRoles.Harvests(ant.Role))
                _harvesters++;
            // O pedido aceito só vale enquanto ela está ociosa indo atendê-lo; ao pegar o bloco, outro estado a conta.
            if (ant.State != AntState.Idle)
                ant.Errand = AntErrand.None;
            else if (ant.Errand == AntErrand.Cut)
                _cutters++;
            else if (ant.Errand == AntErrand.Build)
                _activeBuilders++;
            if (ant.State == AntState.Digging && CellMaterials.IsPlant(_grid.Get(ant.DigX, ant.DigY)))
                _cutters++;
            if (ant.State == AntState.Gathering || ant.State == AntState.Hauling || ant.State == AntState.Placing)
                _activeBuilders++;
            if (ant.State == AntState.Placing)
                _reserved.Add(_grid.ToIndex(ant.DigX, ant.DigY));
            // Com depósito de alimentos, a planta que saiu dele vai aos canteiros; a de fora, ao depósito.
            if (IsHarvesting(ant))
            {
                if (ant.FromStore && FoodDepot)
                {
                    _plantsToBeds += ant.CarriedCount;
                    if (ant.GardenSupply)
                        _gardenPlants += ant.CarriedCount;
                }
                else
                    _plantsInFlight += ant.CarriedCount;
            }
            CountStorage(ant);
            CountSlotOrder(ant);
            CountFarm(ant);
            if (ant.Role == AntRole.Digger)
                _diggers++;
            CountCare(ant);
            CountCultivation(ant);
            if (ant.State == AntState.Fetching)
                _fetchers++;
            else if (ant.State == AntState.Dismembering)
                _dismemberers++;
            CountWasteCarrier(ant);
            CountWasteDirtFetcher(ant);
        }

        /// <summary>Tira a formiga da colônia liberando o que ela reservava (faixa de despejo, ovo ou cria na boca).</summary>
        void RemoveAnt(int index)
        {
            Ant ant = _ants[index];
            DropEquipment(ant);
            ant.Buried = false;
            ReleaseRescue(ant);
            ReleaseDumpSide(ant);
            if (ant.State == AntState.FetchingEgg || ant.State == AntState.CarryingEgg || ant.State == AntState.MovingBrood)
                _brood.ReleaseEgg(ant.EggId, ant.X, ant.Y); // o ovo volta ao chão (ou a cria ao ponto de onde saiu)
            _ants.RemoveAt(index);
        }

        /// <summary>
        /// Sem canteiro esperando planta, a colheita pausa; ela só reabre quando mais da metade dos canteiros espera planta
        /// (descontando os pixels já cortados ou a caminho).
        /// </summary>
        void UpdateHarvestPause()
        {
            if (!_harvestPaused)
                _harvestPaused = !CanHarvest;
            else if ((PlantSupplyDemand - _plantsInFlight - PlantsWaiting) * 2 > HarvestCapacity)
                _harvestPaused = false;
        }

        // ---------------------------------------------------------------- navegação

        void RefreshFields(float deltaTime)
        {
            _fieldTimer -= deltaTime;
            _waterFieldAge += deltaTime;
            ScanStructure();
            bool marksChanged = _grid.MarkVersion != _builtMarkVersion;
            // A rainha a caminho da sala muda a origem dos campos de casa (dentro da sala, a origem é a sala toda).
            bool queenMoved = HomeAnchor() != _homeAnchor;
            // A água que escorre só conta quando algum canteiro espera por ela (o campo da água é refeito).
            bool nutrientsChanged = _grid.NutrientVersion != _builtNutrientVersion || WaterFieldStale ||
                                    _grid.StoreVersion != _builtStoreVersion || _slotClaimHash != _builtSlotClaimHash ||
                                    // As vagas da área de resíduos saem na última etapa, depois do campo da pilha: quando a
                                    // área passa a pedir terra (ou deixa), outra rodada monta (ou para) o campo da pilha.
                                    WastePileWanted != _builtWastePileWanted;
            // Encheu (ou abriu vaga): as plantas marcadas saem (ou voltam) do campo de escavação.
            bool harvestChanged = (CanHarvest, CanStoreDebris, _harvestPaused) != _builtHarvestOpen || RockOpen != _builtRockOpen;
            // Alguém travou ou largou um pixel de planta: os campos precisam deixar de apontar para o travado.
            bool claimsChanged = _claimHash != _builtClaimHash;
            _gardenDirty |= GardenNeedsRefresh(deltaTime) || claimsChanged;

            bool firstBuild = _builtStructureVersion < 0;
            // Só a obra olha qualquer mudança de estrutura (a terra solta de fora é a origem do campo da pilha).
            // A marca que esperava formigas saírem de um bolsão é olhada de novo a cada BuildAntRetrySeconds.
            if (_buildWaitsForAnts)
                _buildAntRetry -= deltaTime;
            bool buildStructure = (_dirtyStructure || _buildWaitsForAnts && _buildAntRetry <= 0f) &&
                                  (_grid.BuildMarkedCount > 0 || _liningSlots.Count > 0);
            // Reboco posto ou desfeito (troca de sólido por sólido, que não muda a estrutura): slots e salas prontas mudam.
            bool liningChanged = _grid.LiningVersion != _liningGridVersion;
            if (!_dirtyUnderground && !_dirtySurface && !_dirtyNearMarks && !buildStructure && !marksChanged && !queenMoved
                && !nutrientsChanged && !harvestChanged && !_gardenDirty && !_nurseryDirty && !_raidDirty && !_dewDirty
                && !DewClaimsChanged && !_farmDirty && !liningChanged)
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
            if (_raidDirty) Profiler.Count(ProfileCounter.CauseRaid);
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
        const int FieldStageCount = 10;
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
                    // Depósitos antes do cultivo: os campos dos canteiros e da água olham as vagas e os itens guardados.
                    BuildStorageFields();
                    // Fazenda depois dos depósitos: o que a fazenda segura (cochos, areia sem depósito) já sai dos itens soltos.
                    BuildFarmFields();
                    BuildNutrientFields();
                    return ProfileStage.FieldNutrients;
                case 6:
                    BuildDigField();
                    return ProfileStage.FieldDig;
                case 7:
                    // Depois dos campos dos slots: o alvo só vale se a sala de nutrientes for alcançável de onde se corta.
                    BuildGardenField();
                    return ProfileStage.FieldGarden;
                case 8:
                    // Slots de reboco antes da obra: eles entram no campo dela.
                    if (LiningStale)
                        UpdateRoomLining();
                    BuildConstructionFields();
                    return ProfileStage.FieldConstruction;
                default:
                    // Por último: o campo dos cupins usa os pontos do berçário e os canteiros já recontados.
                    BuildRaidFields();
                    return ProfileStage.FieldRaid;
            }
        }

        /// <summary>Corrige o campo no que mudou; conta as vezes em que teve de refazer do zero (painel F11).</summary>
        void UpdateField(FlowField field, List<int> sources)
        {
            long start = SimProfiler.Now();
            field.Update(_walkability, sources);
            field.UpdateTicks += SimProfiler.Now() - start;
            field.UpdateCalls++;
            if (!field.LastUpdateWasIncremental)
                Profiler.Count(ProfileCounter.FieldFullBuilds);
        }

        void BuildDigField()
        {
            CountReachableDebris();
            CollectMarkedSources();
            _builtHarvestOpen = (CanHarvest, CanStoreDebris, _harvestPaused);
            UpdateField(_digField, _sources);
            _digSpots = _sources.Count;

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
                            PlantDestinationReachable(nx, ny))
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
            int w = _grid.Width, h = _grid.Height;
            bool[] walkable = _walkability.WalkableCells;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            EnsureNearMarks();
            int chunkColumns = _grid.ChunkColumns;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (!_chunkNearMarks[cy * chunkColumns + cx])
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int cell = y * w + x;
                    if (!walkable[cell])
                        continue;
                    // Descarte barato (a maioria das células de um bloco com marca não encosta em nenhuma): sem marca em volta
                    // não há alvo. Na borda do mundo vale o teste exato abaixo.
                    if (x > 0 && x < w - 1 && y > 0 && y < h - 1 &&
                        !(_grid.IsMarkedAt(cell + 1) || _grid.IsMarkedAt(cell - 1) || _grid.IsMarkedAt(cell + w) ||
                          _grid.IsMarkedAt(cell - w) || _grid.IsMarkedAt(cell + w + 1) || _grid.IsMarkedAt(cell - w + 1) ||
                          _grid.IsMarkedAt(cell + w - 1) || _grid.IsMarkedAt(cell - w - 1)))
                        continue;
                    for (int k = 0; k < 8; k++)
                    {
                        int tx = x + AntNavigation.Dx[k], ty = y + AntNavigation.Dy[k];
                        if (rock ? !IsRockTarget(tx, ty) : !IsWorkTarget(tx, ty, x, y))
                            continue;
                        _sources.Add(cell);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Pixel marcado que a comum em (<paramref name="standX"/>, <paramref name="standY"/>) pode pegar: terra escavável
        /// ou pixel solto de planta (colhido onde parou). O solto pede slot livre e sala de nutrientes alcançável de onde
        /// ela está, para não pegar o que ninguém guardará. Nenhum dos dois pode estar travado (reservado ou sendo cavado)
        /// por outra formiga.
        /// </summary>
        bool IsWorkTarget(int x, int y, int standX, int standY)
        {
            if (!_grid.IsMarked(x, y) || _claimed.Contains(_grid.ToIndex(x, y)))
                return false;
            CellMaterial material = _grid.Get(x, y);
            if (CellMaterials.IsDiggable(material))
                return true;
            return CellMaterials.IsPlantDebris(material) && CanStoreDebris &&
                   PlantDestinationReachable(standX, standY);
        }

        /// <summary>
        /// Conta os pixels de planta soltos que alguma formiga pode pegar: marcados (só o marcado é colhido) e com um vizinho
        /// caminhável ligado à rainha pelo campo de casa. Roda a cada rodada de campos, depois do campo de casa; só visita os
        /// blocos que têm pixel solto.
        /// </summary>
        void CountReachableDebris()
        {
            _reachableDebris = 0;
            if (_grid.PlantDebrisCount == 0)
                return;
            int w = _grid.Width, h = _grid.Height;
            for (int cy = 0; cy < _grid.ChunkRows; cy++)
            for (int cx = 0; cx < _grid.ChunkColumns; cx++)
            {
                if (_grid.GetChunkPlantDebrisCount(cx, cy) == 0)
                    continue;
                int x1 = Mathf.Min(w, (cx + 1) << PixelGrid.ChunkShift), y1 = Mathf.Min(h, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = cy << PixelGrid.ChunkShift; y < y1; y++)
                for (int x = cx << PixelGrid.ChunkShift; x < x1; x++)
                {
                    if (!CellMaterials.IsPlantDebris(_grid.Get(x, y)) || !_grid.IsMarked(x, y))
                        continue;
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                        if (!_grid.InBounds(nx, ny) || !_walkability.IsWalkable(nx, ny) || _homeField.Get(nx, ny) == FlowField.Unreachable)
                            continue;
                        _reachableDebris++;
                        break;
                    }
                }
            }
        }

        /// <summary>A célula de índice <paramref name="index"/> tem um pixel de planta solto (folha caída, grama cortada...).</summary>
        bool IsDebrisAt(int index) => CellMaterials.IsPlantDebris(_grid.Get(index % _grid.Width, index / _grid.Width));

        /// <summary>Rocha marcada que a escavadora pode quebrar, sem outra já quebrando a mesma.</summary>
        bool IsRockTarget(int x, int y) =>
            _grid.IsMarked(x, y) && _grid.Get(x, y) == CellMaterial.Rock && !_claimed.Contains(_grid.ToIndex(x, y));

        /// <summary>Trava o pixel que a formiga está cavando/cortando/pegando: terra, planta (em pé ou solta) ou rocha.</summary>
        void ClaimTarget(Ant ant)
        {
            CellMaterial material = _grid.Get(ant.DigX, ant.DigY);
            if (!CellMaterials.IsDiggable(material) && !CellMaterials.IsPlant(material) && !CellMaterials.IsPlantDebris(material) &&
                material != CellMaterial.Rock)
                return;
            Claim(_grid.ToIndex(ant.DigX, ant.DigY));
        }

        /// <summary>Trava a célula para as outras formigas; os campos deixam de apontar para ela na próxima rodada.</summary>
        void Claim(int index)
        {
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
        /// têm caminho até a sala de nutrientes, a mais perto da entrada do formigueiro; no empate, a maior. Ela fica
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
            _gardenCandidates.Sort((a, b) => a.z != b.z ? a.z.CompareTo(b.z) : a.y != b.y ? b.y.CompareTo(a.y) : a.x.CompareTo(b.x));
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
                        PlantDestinationReachable(nx, ny))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Quanto o papel encurta (ou não) o tempo de escavar, construir ou cortar a célula <paramref name="material"/>.
        /// As galerias separam planta (corte: O+3, F+1) de terra (escavação: E+1).
        /// </summary>
        float WorkTimeFactor(Ant ant, CellMaterial material)
        {
            float factor = AntRoles.DigTimeFactor(ant.Role) * Tree.DigTimeFactor(ant.Role) * AntRoles.CutTimeFactor(ant.Role) *
                           Tree.CutTimeFactor(ant.Role);
            bool plant = CellMaterials.IsPlant(material);
            if (!plant)
                factor *= Effects.AllDigTime * ToolFactor(ant);
            switch (ant.Role)
            {
                case AntRole.Worker: return plant ? factor * Effects.WorkerCutTime : factor;
                case AntRole.Cutter: return plant ? factor * Effects.CutterCutTime : factor;
                case AntRole.Digger: return plant ? factor : factor * Effects.DiggerDigTime;
                default: return factor;
            }
        }

        /// <summary>Leva planta na mão (cada pixel ocupa um slot da sala de nutrientes até ser guardado). O pixel solto, no chão, reserva via PlantDebrisCount.</summary>
        bool IsHarvesting(Ant ant) => CellMaterials.IsPlant(ant.Carried);

        // ---------------------------------------------------------------- nutrientes

        /// <summary>Teto de formigas buscando nutrientes ao mesmo tempo (o resto continua cavando e colhendo).</summary>
        int MaxFetchers => Mathf.Max(1, _ants.Count / 2);

        /// <summary>Ociosa: se houver nutriente guardado com caminho até a rainha e ninguém indo buscá-lo, vai buscar.</summary>
        bool TryStartFetch(Ant ant)
        {
            if (!SectionAllows(WorkSection.QueenFeed, ant))
                return false;
            // A carregadora busca mesmo com o teto de buscadoras cheio: transportar é o trabalho dela.
            bool capped = _fetchers >= MaxFetchers && ant.Role != AntRole.Carrier;
            if (!_nutrientsReachQueen || _fetchers >= _grid.NutrientCount || capped)
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
            if (!TakeNutrientNear(ant))
                return false;
            ant.State = AntState.Delivering;
            ant.Cooldown = _settings.DropSeconds;
            return true;
        }

        /// <summary>Tira da sala o nutriente na célula da formiga ou numa das 8 vizinhas.</summary>
        bool TakeNutrientNear(Ant ant)
        {
            for (int k = -1; k < 8; k++)
            {
                int x = k < 0 ? ant.X : ant.X + AntNavigation.Dx[k];
                int y = k < 0 ? ant.Y : ant.Y + AntNavigation.Dy[k];
                if (!_grid.HasNutrient(x, y))
                    continue;
                _grid.SetNutrient(x, y, false);
                return true;
            }
            return false;
        }

        float _nutrientRemainder;

        /// <summary>
        /// Credita um nutriente entregue: vale 1, mais C3 (+20% por planta) e C+2 (+5% por nível); a fração que sobra fica para a
        /// próxima entrega, para o saldo continuar inteiro.
        /// </summary>
        void CreditNutrient()
        {
            _nutrientRemainder += Effects.PlantYield * Effects.DeliveryValue;
            int whole = (int)_nutrientRemainder;
            _nutrientRemainder -= whole;
            Nutrients += whole;
            TotalNutrients += whole;
        }

        /// <summary>
        /// A própria rainha ociosa vai à sala de nutrientes e come o que está guardado (pedido do desenvolvedor em
        /// 2026-10-18). Desde 2026-10-19 só nos trabalhos COLETAR RECURSOS e COLETAR NUTRIENTE, ou sem nenhuma adulta na
        /// colônia; no ninho, as formigas entregam. Devolve true se o passo foi gasto nisso.
        /// </summary>
        bool TryQueenEatNutrient(Ant queen)
        {
            if (queen.State != AntState.Idle || _grid.NutrientCount <= 0)
                return false;
            int d = _nutrientField.Get(queen.X, queen.Y);
            if (d == FlowField.Unreachable)
                return false;
            if (d == 0)
            {
                if (!TakeNutrientNear(queen))
                    return false;
                CreditNutrient();
                EmitFx(FxId.NutrientGain, queen.X, queen.Y);
                queen.Cooldown = _settings.DropSeconds;
                return true;
            }
            return StepDownhill(queen, _nutrientField);
        }

        /// <summary>
        /// Leva o nutriente até a rainha: desce o campo de casa (até a sala dela, ou até ela fora da sala) e, dentro da
        /// sala, chega perto dela. Encostou (8 vizinhas), ela consome: +1 nutriente.
        /// </summary>
        void UpdateDelivering(Ant ant)
        {
            if (Mathf.Abs(ant.X - Queen.X) <= 1 && Mathf.Abs(ant.Y - Queen.Y) <= 1)
            {
                CreditNutrient();
                EmitFx(FxId.NutrientGain, Queen.X, Queen.Y);
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
            if (!SectionAllows(WorkSection.Nursery, ant) || _nurserySpots.Count == 0 || !_brood.HasEggToCarry || _homeField.Get(ant.X, ant.Y) == FlowField.Unreachable)
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
            _buildWaitsForAnts = false;
            _buildAntRetry = BuildAntRetrySeconds;
            _buildSources.Clear();
            _siteSources.Clear();
            _hasBuildWork = false;
            _hasSite = false;
            _liningReachable = 0;
            if (_grid.BuildMarkedCount == 0 && _liningSlots.Count == 0)
            {
                // Parados, os campos deixam de acompanhar as mudanças de caminhabilidade: refazem do zero quando voltar a obra.
                _exitField.Invalidate();
                _pileField.Invalidate();
                _buildField.Invalidate();
                _siteField.Invalidate();
                _buildFieldsVersion = -1;
                _buildDepth.Clear();
                _mushroomSiteSources.Clear();
                UpdateMushroomSiteField(0);
                return;
            }
            DropStaleBuildDepths();

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
            _mushroomSiteSources.Clear();
            int mushroomSites = 0;
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
                    // Marca de cogumelo: só quem leva cogumelo a preenche (campo próprio); a terra fica com as outras marcas.
                    bool mushroom = _grid.IsGlowMushroomMarked(x, y);
                    if (mushroom && fillable)
                        mushroomSites++;
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
                        if (mushroom)
                        {
                            if (fillable && !_grid.IsBuildMarked(nx, ny))
                                _mushroomSiteSources.Add(ny * w + nx);
                            continue;
                        }
                        // Do lado de fora, não de cima de outra marca: parada nela, a formiga tampa o ponto de outra (que pode
                        // estar tampando o dela) e as duas esperam para sempre; a obra é de baixo para cima, sem atalho.
                        if (fillable && !(IsOutdoor(nx, ny) && _grid.IsBuildMarked(nx, ny)))
                            _buildSources.Add(ny * w + nx);
                        // Fila de espera: fora das marcas, para a formiga com terra não ficar em cima do que vai ser preenchido.
                        if (!_grid.IsBuildMarked(nx, ny))
                            _siteSources.Add(ny * w + nx);
                    }
                }
            }
            AddLiningSources();
            UpdateField(_buildField, _buildSources);
            UpdateField(_siteField, _siteSources);
            _hasBuildWork = _buildSources.Count > 0;
            _hasSite = _hasBuildWork || _siteSources.Count > 0;
            UpdateMushroomSiteField(mushroomSites);
        }

        /// <summary>Raio (em células) em que uma marca mais funda segura o preenchimento de uma mais rasa.</summary>
        const int BuildOrderRadius = 2;
        /// <summary>Quanto mais funda (distância até a saída) pode ser uma marca vizinha sem segurar a célula: dá vários pontos de trabalho de uma vez.</summary>
        const int BuildOrderSlack = 1;
        /// <summary>Teto de células da varredura de bolsão; acima disso o espaço conta como aberto.</summary>
        const int PocketScanLimit = 1500;
        /// <summary>De quanto em quanto tempo (s simulados) a obra olha de novo as marcas que esperam formigas saírem de um bolsão.</summary>
        const float BuildAntRetrySeconds = 1f;
        /// <summary>Alguma marca deixou de ser preenchida só porque fecharia formigas num bolsão (<see cref="PocketHasAnts"/>).</summary>
        bool _buildWaitsForAnts;
        float _buildAntRetry;

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
            // Do lado de fora a obra cresce a partir do que já existe: a célula precisa encostar em algo sólido, então o
            // apoio vem de graça e a ordem de baixo para cima também. Na arte do cenário a célula vazia é caminhável sem
            // apoio (PixelGrid.IsBackdrop), por isso o encosto é conferido aqui e não só pelo campo. A regra de
            // profundidade vale só dentro do formigueiro.
            bool outdoor = IsOutdoor(x, y);
            if (outdoor && !AntNavigation.TouchesSolid(_grid, x, y))
                return false;
            // O encosto pode ser na diagonal; com a célula de baixo ainda marcada e vazia, espera por ela: dentro da obra,
            // de baixo para cima.
            if (outdoor && _grid.IsBuildMarked(x, y - 1) && !_grid.IsSolid(x, y - 1))
                return false;
            int depth = OrderDepth(x, y, d);
            for (int dy = -BuildOrderRadius; dy <= BuildOrderRadius && !outdoor; dy++)
            for (int dx = -BuildOrderRadius; dx <= BuildOrderRadius; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (!_grid.IsBuildMarked(nx, ny) || _grid.IsSolid(nx, ny) || IsOutdoor(nx, ny))
                    continue;
                // A marca de cogumelo espera o cogumelo (e a de terra, a terra): uma não segura a ordem da outra.
                if (_grid.IsGlowMushroomMarked(nx, ny) != _grid.IsGlowMushroomMarked(x, y))
                    continue;
                int nd = _exitField.Get(nx, ny);
                if (nd != FlowField.Unreachable && OrderDepth(nx, ny, nd) > depth + BuildOrderSlack)
                    return false;
            }
            return !BlocksPassage(x, y);
        }

        /// <summary>
        /// Profundidade da marca para a ordem da obra: a distância até a saída na primeira vez em que ela é vista, mantida
        /// depois. A distância de agora cresce conforme a obra fecha o túnel em volta (um vizinho preenchido obriga a
        /// contornar), e uma marca rasa passaria a parecer tão funda quanto as do fundo e seria preenchida antes delas.
        /// </summary>
        int OrderDepth(int x, int y, int exitDistance)
        {
            int index = _grid.ToIndex(x, y);
            if (_buildDepth.TryGetValue(index, out int depth))
                return depth;
            _buildDepth[index] = exitDistance;
            return exitDistance;
        }

        /// <summary>Esquece a profundidade das marcas que sumiram (preenchidas ou apagadas), para uma marca nova no lugar medir de novo.</summary>
        void DropStaleBuildDepths()
        {
            if (_buildDepth.Count <= _grid.BuildMarkedCount)
                return;
            _buildDepthStale.Clear();
            int w = _grid.Width;
            foreach (int index in _buildDepth.Keys)
            {
                int x = index % w, y = index / w;
                if (!_grid.IsBuildMarked(x, y) || _grid.IsSolid(x, y))
                    _buildDepthStale.Add(index);
            }
            foreach (int index in _buildDepthStale)
                _buildDepth.Remove(index);
        }

        /// <summary>
        /// Preencher (x, y) isolaria algum espaço que ainda tem marca de construção, formiga ou a rainha? Olha as 8
        /// vizinhas vazias: se ficarem em um grupo só (movimento em 8 direções), nada se isola. Se houver mais de um grupo,
        /// cada um que não é o da saída é varrido (limitado): se não achar caminho até o grupo da saída e tiver marca ou
        /// formiga dentro, bloqueia (KI-006: antes só a marca contava, e a obra trancava quem estava no fundo de um beco).
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
                if (first && PocketMustStayOpen(x, y, i, mainRoot, n))
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
        /// Devolve true se ficar preso sem chegar ao grupo da saída e houver marca de construção, formiga ou a rainha lá dentro.
        /// </summary>
        bool PocketMustStayOpen(int blockX, int blockY, int start, int mainRoot, int ringCount)
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
            if (marks)
                return true;
            if (!PocketHasAnts(stamp))
                return false;
            // As formigas andam sem mudar a estrutura: sem isto, a marca só seria olhada de novo na próxima mudança da grade.
            _buildWaitsForAnts = true;
            return true;
        }

        /// <summary>Alguma formiga (ou a rainha) está numa célula da última varredura (<paramref name="stamp"/>)? A soterrada não conta: ela está na terra.</summary>
        bool PocketHasAnts(int stamp)
        {
            int w = _grid.Width;
            if (!Queen.Buried && _grid.InBounds(Queen.X, Queen.Y) && _floodStamp[Queen.Y * w + Queen.X] == stamp)
                return true;
            foreach (Ant ant in _ants)
            {
                if (!ant.Buried && _grid.InBounds(ant.X, ant.Y) && _floodStamp[ant.Y * w + ant.X] == stamp)
                    return true;
            }
            return false;
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

        /// <summary>
        /// Origens = células caminháveis encostadas em terra solta acima do solo original (a pilha de fora). Por dentro da
        /// pilha também se anda (<see cref="AntNavigation.IsOutdoorWall"/>): a formiga pega o grão de onde estiver.
        /// </summary>
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
        // Blocos com marca de escavação em si ou num vizinho, recalculados só quando as marcas ou a estrutura mudam (varrer todos
        // os blocos com HasMarksAround a cada rodada custava ~1,6 ms por coleta, mesmo com 5 marcas no mapa).
        bool[] _chunkNearMarks;
        int _nearMarksMarkVersion = -1, _nearMarksStructureVersion = -1;

        void EnsureNearMarks()
        {
            if (_chunkNearMarks == null)
                _chunkNearMarks = new bool[_grid.ChunkColumns * _grid.ChunkRows];
            if (_nearMarksMarkVersion == _grid.MarkVersion && _nearMarksStructureVersion == _grid.StructureVersion)
                return;
            _nearMarksMarkVersion = _grid.MarkVersion;
            _nearMarksStructureVersion = _grid.StructureVersion;
            System.Array.Clear(_chunkNearMarks, 0, _chunkNearMarks.Length);
            int columns = _grid.ChunkColumns, rows = _grid.ChunkRows;
            for (int cy = 0; cy < rows; cy++)
            for (int cx = 0; cx < columns; cx++)
            {
                if (_grid.GetChunkMarkedCount(cx, cy) == 0)
                    continue;
                for (int ny = Mathf.Max(0, cy - 1); ny <= Mathf.Min(rows - 1, cy + 1); ny++)
                for (int nx = Mathf.Max(0, cx - 1); nx <= Mathf.Min(columns - 1, cx + 1); nx++)
                    _chunkNearMarks[ny * columns + nx] = true;
            }
        }

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
            // A faixa é todo o céu caminhável das colunas dela (milhares de origens): sem mudança de caminhabilidade nem de
            // estrutura desde a rodada em que foi montada, o campo já está certo e a faixa é pulada (eram ~5 ms por rodada).
            bool walkabilityChanged = _walkability.ChangedCells.Count > 0 || _walkability.ChangesOverflowed;
            foreach (DumpSide side in _dumpSides)
            {
                if (!walkabilityChanged && side.SourcesVersion == _grid.StructureVersion)
                    continue;
                side.SourcesVersion = _grid.StructureVersion;
                List<int> sources = side.Sources;
                sources.Clear();
                for (int x = side.MinX; x <= side.MaxX; x++)
                for (int y = Mathf.Max(0, _grid.GetSurfaceY(x) + 1); y < _grid.Height; y++)
                {
                    if (_walkability.IsWalkable(x, y))
                        sources.Add(_grid.ToIndex(x, y));
                }
                UpdateField(side.Field, sources);
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
            int maxOpenings = _settings.MaxDumpOpenings + Effects.DumpOpeningsBonus;
            if (_dumpSides.Count > 0 && area.xMin == _sidesArea.xMin && area.xMax == _sidesArea.xMax &&
                maxOpenings == _openingsLimit && SameOpenings(_scannedOpenings, _allOpenings))
                return false;
            _openingsLimit = maxOpenings; // T1 abre mais saídas: o limite novo força refazer as faixas

            // Os índices antigos deixam de valer; as faixas novas começam sem ninguém a caminho.
            foreach (Ant ant in _ants)
            {
                ant.DumpSide = -1;
                ant.GuardGroup = -1; // os grupos de guarda são as aberturas: quem estava num posto escolhe de novo
            }
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
            if (_openings.Count > maxOpenings)
                _openings.RemoveRange(maxOpenings, _openings.Count - maxOpenings);

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
            // Conta pixels, não formigas: as cargas variam (1 a 3+ pixels) e o monte precisa equilibrar o que chega.
            ant.DumpPixels = ant.CarriedCount;
            target.InFlight += ant.DumpPixels;
        }

        void ReleaseDumpSide(Ant ant)
        {
            if (ant.DumpSide >= 0)
                _dumpSides[ant.DumpSide].InFlight -= ant.DumpPixels;
            ant.DumpSide = -1;
            ant.DumpPixels = 0;
        }

        // ---------------------------------------------------------------- rainha

        /// <summary>
        /// Acha os grupos conectados (8 direções) de células pintadas como sala da rainha e caminháveis, e fica com o
        /// maior; no empate, com o que já tem a rainha, senão o primeiro achado. Monta o campo até essa sala.
        /// </summary>
        void UpdateQueenRoom()
        {
            _previousRoomCells.Clear();
            _previousRoomCells.AddRange(_roomCells);
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
            DirtyQueenRoomRender();
        }

        /// <summary>A sala da rainha é desenhada com mais força que as outras pinturas: o render refaz os blocos onde ela mudou.</summary>
        void DirtyQueenRoomRender()
        {
            bool same = _previousRoomCells.Count == _roomCells.Count;
            for (int i = 0; same && i < _roomCells.Count; i++)
                same = _previousRoomCells[i] == _roomCells[i];
            if (same)
                return;
            int w = _grid.Width;
            for (int i = 0; i < _previousRoomCells.Count; i++)
                _grid.DirtyRender(_previousRoomCells[i] % w, _previousRoomCells[i] / w);
            for (int i = 0; i < _roomCells.Count; i++)
                _grid.DirtyRender(_roomCells[i] % w, _roomCells[i] / w);
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
        /// Passo da rainha: caça (ofensiva), cria sozinha, o trabalho da classe escolhida e, sem nada disso, a ociosidade
        /// (<see cref="QueenIdle"/>).
        /// </summary>
        void UpdateQueen(float deltaTime)
        {
            Ant queen = Queen;
            // A IA das formigas lê o papel: trabalhando, a rainha tem o da classe escolhida (no ninho, o da comum).
            QueenJobs.TryGetRole(QueenJob, out AntRole role);
            queen.Role = role;
            if (UpdateSupport(queen, deltaTime))
                return;
            queen.Cooldown -= deltaTime;
            if (queen.Cooldown > 0f)
                return;

            // Ofensiva, com invasor no formigueiro, sai da sala atrás dele (volta à sala depois, pelo caminho de sempre).
            if (QueenStance == QueenStance.Offensive && _nestIntruders > 0 && HuntIntruder(queen))
                return;

            if (TickLoneQueenBrood(queen))
                return;

            if (TickQueenWork(queen))
                return;

            // No ninho, quem leva o nutriente são as formigas (seção Alimentar a rainha); ir comer na sala de nutrientes é
            // o trabalho COLETAR NUTRIENTE (decisão do desenvolvedor em 2026-10-19). Só a rainha sem nenhuma adulta ainda
            // vai comer sozinha: senão ninguém traria nutriente e a colônia travaria.
            if (_ants.Count == 0 && TryQueenEatNutrient(queen))
                return;

            QueenIdle(queen);
        }

        /// <summary>
        /// Rainha ociosa (pedido do desenvolvedor em 2026-10-21): só fica parada na sala da rainha. Com sala alcançável, vai
        /// até ela pelo caminho e, dentro, anda só pelas células da sala, com paradas de vez em quando; sem sala (ou sem
        /// caminho até ela), anda pelo formigueiro como uma formiga ociosa.
        /// </summary>
        void QueenIdle(Ant queen)
        {
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
            // Sem sala, a casa das formigas é a própria rainha: do lado de fora, ela volta sozinha pela abertura mais perto.
            if (IsOutdoor(queen.X, queen.Y) && StepTowardsOpening(queen))
                return;
            WanderHome(queen);
        }

        /// <summary>Um passo rumo à abertura do formigueiro mais perto (o meio dela, na superfície); <c>false</c> sem abertura ou sem passo.</summary>
        bool StepTowardsOpening(Ant ant)
        {
            int bestX = -1, best = int.MaxValue;
            foreach (Vector2Int opening in _openings)
            {
                int x = (opening.x + opening.y) / 2;
                int distance = Mathf.Abs(x - ant.X);
                if (distance < best)
                {
                    best = distance;
                    bestX = x;
                }
            }
            return bestX >= 0 && StepTowardsCell(ant, bestX, _grid.GetSurfaceY(bestX), keepQueenRoom: false);
        }

        /// <summary>
        /// Rainha sozinha no formigueiro: leva o ovo que pôs ao berçário e cuida dele (pedido do desenvolvedor em 2026-10-18).
        /// Com formigas de volta, termina o que já carrega e deixa o cuidado para elas. Devolve true se o passo foi gasto nisso.
        /// </summary>
        bool TickLoneQueenBrood(Ant queen)
        {
            // Trabalhando como uma classe, com adultas na colônia, o berçário dela segue a IA do papel (TickQueenWork).
            if (QueenJob != QueenJob.Nest && _ants.Count > 0)
                return false;
            switch (queen.State)
            {
                case AntState.FetchingEgg:
                    UpdateFetchingEgg(queen);
                    return true;
                case AntState.CarryingEgg:
                    UpdateCarryingEgg(queen);
                    return true;
                case AntState.MovingBrood:
                    UpdateMovingBrood(queen);
                    return true;
                case AntState.Tending:
                    if (_ants.Count > 0)
                        Leave(queen, ref _nursery);
                    else if (!QuitStoppedSection(queen))
                        UpdateTending(queen);
                    return true;
            }
            if (_ants.Count > 0)
                return false;
            return TryStartEggFetch(queen) || TryStartCare(queen);
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
            {
                // A rainha e o corpo dos cupins (velocidade própria) seguem sendo empurrados para cima; as formigas ficam presas sem colisão.
                if (ReferenceEquals(ant, Queen) || ant.SpeedOverride > 0f)
                {
                    PushOutOfSolid(ant);
                }
                else
                {
                    if (!ant.Buried)
                        Bury(ant);
                    return true;
                }
            }
            else if (ant.Buried)
            {
                ant.Buried = false; // o espaço em volta abriu: volta a andar
                ReleaseRescue(ant);
            }

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

        void StepAnt(Ant ant, float deltaTime)
        {
            if (UpdateSupport(ant, deltaTime))
                return;

            // Cupim no chão: as guerreiras (soldado, cuspideira) largam o trabalho na hora e vão caçá-lo; as demais seguem
            // trabalhando e só lutam quando um cupim as morde. Com cupim dentro do formigueiro, todas largam o trabalho e vão atrás dele (pedido do desenvolvedor
            // em 2026-10-18).
            if (ant.State != AntState.Fighting &&
                (_groundTermites > 0 && (AntRoles.IsWarrior(ant.Role) || _nestIntruders > 0 || IsUnderAttack(ant)) ||
                 _postingActive && AntRoles.IsWarrior(ant.Role)))
                InterruptForCombat(ant);

            ant.Cooldown -= deltaTime;
            if (ant.Cooldown > 0f)
                return;

            // Seção desligada (ou acima do limite) no painel da colônia: quem só cuida sai na hora.
            if (QuitStoppedSection(ant))
                return;
            RunState(ant);
        }

        /// <summary>Um passo do estado da formiga (já com apoio e sem espera); ociosa, escolhe o que fazer.</summary>
        void RunState(Ant ant)
        {
            switch (ant.State)
            {
                case AntState.Fighting:
                    UpdateFighting(ant);
                    break;
                case AntState.Rescuing:
                    UpdateRescuing(ant);
                    break;
                case AntState.Dismembering:
                    UpdateDismembering(ant);
                    break;
                case AntState.CarryingWaste:
                    UpdateCarryingWaste(ant);
                    break;
                case AntState.TakingStored:
                    UpdateTaking(ant);
                    break;
                case AntState.Carrying:
                    if (ant.StoreTarget != StorageKind.None)
                        UpdateStocking(ant);
                    else if (CellMaterials.IsPlant(ant.Carried))
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
                case AntState.TendingDew:
                    UpdateTendingDew(ant);
                    break;
                case AntState.FeedingAphid:
                    UpdateFeedingAphid(ant);
                    break;
                case AntState.TendingAphid:
                    UpdateTendingAphid(ant);
                    break;
                case AntState.BringingSoil:
                    UpdateBringingSoil(ant);
                    break;
                case AntState.FetchingSoil:
                    UpdateFetchingSoil(ant);
                    break;
                case AntState.GatheringSoil:
                    FinishSoilGather(ant);
                    break;
                case AntState.FetchingWater:
                    UpdateFetchingWater(ant);
                    break;
                case AntState.CarryingWater:
                    if (ant.StoreTarget == StorageKind.Water)
                        UpdateStocking(ant);
                    else
                        UpdateCarryingWater(ant);
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
                    _seekKept = false;
                    _errandKept = false;
                    UpdateIdle(ant);
                    // Foi fazer outra coisa (ovo, cuidado, nutriente...): a reserva e o pedido ficam livres para as outras.
                    if (!_seekKept)
                        ant.ClearSeek();
                    if (!_errandKept)
                        ant.Errand = AntErrand.None;
                    break;
            }
        }

        /// <summary>Até onde (em passos do campo de escavação) uma formiga com a mão não cheia vai buscar mais um pixel antes de entregar.</summary>
        const int LoadedSeekRange = 4;

        /// <summary>
        /// Sino do HUD: "volte para o formigueiro". As formigas terminam o que têm nas mãos e depois só vagam pelo formigueiro,
        /// sem começar trabalho novo; a cuidadora segue no berçário e os soldados seguem lutando. Não vai para o save.
        /// </summary>
        public bool Recalled { get; set; }

        void UpdateIdle(Ant ant)
        {
            if (Recalled && ant.CarriedCount == 0 && ant.Role != AntRole.Nurse)
            {
                ant.ActingAsWorker = false;
                _idleStroll = true;
                WanderHome(ant);
                _idleStroll = false;
                return;
            }

            // Mão com pixels e sem alvo ao alcance: procura mais um perto; senão entrega o que tem (seguindo o trabalho
            // em que pegou a carga, do papel ou da comum).
            if (ant.CarriedCount > 0)
            {
                UpdateLoadedIdle(ant);
                return;
            }

            // De mão vazia, decide de novo a cada passo, sempre começando pelo trabalho do próprio papel.
            ant.ActingAsWorker = false;

            _stepMark = 1;
            if (TryTaskWork(ant))
                return;

            // Sem trabalho do papel (soldado sem combate, cortadeira sem corte...), faz o da comum (pedido do
            // desenvolvedor em 2026-10-19). Velocidade, carga, vida e ritmos continuam os do papel. Antes da ordem fixa da
            // comum, tenta a segunda função: a seção que mais precisa de formigas agora (KI-033, 2026-10-20).
            ant.SecondFunction = -1;
            if (ant.Role != AntRole.Worker)
            {
                ant.ActingAsWorker = true;
                _stepMark = 2;
                if (TrySecondFunction(ant))
                    return;
                _stepMark = 3;
                if (TryTaskWork(ant))
                    return;
                ant.ActingAsWorker = false;
            }

            _stepMark = 4;
            IdleStroll(ant);
        }

        /// <summary>Verdadeiro só durante o passo de quem vaga sem trabalho: <see cref="SpeedOf"/> aplica o ritmo ocioso.</summary>
        bool _idleStroll;

        /// <summary>Sem trabalho nenhum: vaga (a rainha, pela sala) mais devagar (<see cref="ColonySettings.IdleSpeedRate"/>, desde 2026-10-21).</summary>
        void IdleStroll(Ant ant)
        {
            _idleStroll = true;
            if (ant == Queen)
                QueenIdle(ant);
            else
                WanderHome(ant);
            _idleStroll = false;
        }

        /// <summary>
        /// Começa (ou segue) o trabalho que <see cref="Ant.TaskRole"/> faz, na ordem de prioridade dele. <c>false</c> = nada
        /// a fazer para esse papel agora. As seções desligadas ou cheias no painel da colônia ficam de fora (cada TryStart*
        /// confere a sua).
        /// </summary>
        bool TryTaskWork(Ant ant)
        {
            AntRole role = ant.TaskRole;

            // Soterrada por perto: com O3, a operária ociosa vai tirá-la antes de tudo.
            _stepMark = 10;
            if (role == AntRole.Worker && TryStartRescue(ant))
                return true;

            // Sem o equipamento do papel, busca no armazém antes de trabalhar (documento 45).
            if (TryStartEquip(ant))
                return true;

            // As guerreiras só lutam: fora do ataque, tiram as partes dos cupins mortos.
            if (AntRoles.IsWarrior(role))
                return TryStartDismember(ant);

            // A lixeira vive dos resíduos: corpos de cupim, itens soltos (as folhas dos restos, a água e a areia que a área
            // devolve) e a terra que a área de terra → água pede. Sem nada disso, faz o trabalho da comum.
            _stepMark = 11;
            if (role == AntRole.Midden)
                return TryStartDismember(ant) || TryStartLoose(ant) || TryStartWasteRestock(ant);

            // A fazendeira vive na fazenda de pulgões: enche os cochos e ordenha. Sem nada disso, faz o trabalho da comum.
            _stepMark = 28;
            if (AntRoles.Farms(role))
                return TryFarmWork(ant);

            // A carregadora vive do estoque (pedido do desenvolvedor em 2026-10-21): primeiro abastece os depósitos, depois
            // entrega o que está neles. Sem nada disso, faz o trabalho da comum (segunda função).
            _stepMark = 27;
            if (role == AntRole.Carrier)
                return TryStockWork(ant) || TryDeliverFromStock(ant);

            // A cortadeira só corta: quem busca nutriente, recolhe pixel solto, cava e constrói são as outras.
            _stepMark = 12;
            if (AntRoles.CutsFirst(role))
                return TryCutterWork(ant);

            // Construção primeiro (pedido do desenvolvedor em 2026-10-22): a obra marcada e o reboco das salas pintadas são
            // escolhas pontuais do jogador; cuidado, ovo, rega, comida da rainha e o abastecimento automático vêm depois.
            // Só a comum (e quem age como ela): a escavadora cava antes de construir, cuidadora e jardineira ficam no papel.
            _stepMark = 22;
            if (role == AntRole.Worker && _hasSite && TryBuildWork(ant))
                return true;
            // A comum também abastece a fundição com o minério e a serapilheira dos depósitos.
            if (role == AntRole.Worker && TryStartWorkshopSupply(ant))
                return true;

            // Ovo vem antes de tudo; a escavadora não leva ovo nem nutriente, ela prioriza as obras.
            // A especialista (cuidadora, jardineira) vai à região antes de tudo; a comum, logo depois do ovo e antes das obras.
            bool specialist = role == AntRole.Nurse || role == AntRole.Gardener;
            _stepMark = 13;
            if (specialist && TryStartCare(ant))
                return true;

            _stepMark = 14;
            if (AntRoles.CarriesEggs(role) && TryStartEggFetch(ant))
                return true;

            _stepMark = 15;
            if (!specialist && TryStartCare(ant))
                return true;

            // A cuidadora vive no berçário: sem cria para cuidar nem ovo para levar, não tem trabalho do papel.
            if (role == AntRole.Nurse)
                return false;

            // Regar vem logo depois do cuidado: canteiro com planta parado sem água segura a produção. Com depósito, quem
            // trabalha na sala de nutrientes busca nele a água e a planta dos canteiros.
            _stepMark = 16;
            if (TryStartGardenSupply(ant) || AntRoles.CarriesWater(role) && TryStartWaterFetch(ant))
                return true;

            // Com depósito de alimentos, a folha da sala Gotas de Orvalho vem de lá (a de fora vai ao depósito).
            _stepMark = 17;
            if (TryStartPlantFromDepot(ant))
                return true;

            _stepMark = 18;
            if (role == AntRole.Gardener)
            {
                if (_grid.NutrientCount > 0 && TryStartFetch(ant))
                    return true;
                if (TryStartWaterStock(ant))
                    return true;
                // Sala Gotas de Orvalho: terra fofa da pilha de fora e, depois dela, a folha cortada da planta.
                if (TryStartSoilFetch(ant, dewOnly: true))
                    return true;
                if (_dewLeafSpots > 0 && !_harvestPaused && TryCutWork(ant))
                    return true;
                // Sala pronta e sem mais o que fazer: fica ao lado da queda das gotas, acelerando-as.
                return TryStartDewCare(ant);
            }

            // Corpos de cupim: a comum e a carregadora os desmembram antes de buscar nutriente (as folhas viram planta).
            _stepMark = 19;
            if (TryStartDismember(ant))
                return true;

            _stepMark = 20;
            if (AntRoles.FetchesNutrients(role) && _grid.NutrientCount > 0 && TryStartFetch(ant))
                return true;

            _stepMark = 21;
            if (_grid.MarkedCount > 0 && SectionAllows(WorkSection.Dig, ant))
            {
                // Rocha marcada: só a escavadora com E2, e ela vai à pedra antes da terra da mesma trilha (a que já
                // reservou terra termina de buscá-la antes).
                bool seekingSoil = ant.SeekTarget >= 0 && !ant.SeekRock;
                if (RockOpen && Tree.CanBreakRock(ant.Role) && !seekingSoil && SeekWork(ant, _rockField, rock: true, FlowField.Unreachable))
                    return true;
                if (SeekWork(ant, _digField, rock: false, FlowField.Unreachable))
                    return true;
            }

            _stepMark = 23;
            if (_hasSite && TryBuildWork(ant))
                return true;
            // Marca de cogumelo luminoso: o cogumelo vem dos pulgões (depósito de construção ou solto na fazenda).
            if (TryStartMushroomWork(ant))
                return true;

            // Itens soltos (depósito apagado, folhas do resto de cupim) e encher o depósito de água com a água do mapa.
            _stepMark = 24;
            if (TryStartLoose(ant) || TryStartWaterStock(ant))
                return true;

            // Sem mais nada a fazer, a que também corta (comum, carregadora) vai à planta-alvo.
            // Sem terra cavada chegando, a terra fofa dos canteiros vem da pilha de fora (antes de cortar: sem canteiro, a
            // colheita fica pausada).
            // Depois do depósito de construção (que só chama formigas para as vagas dele): no modo terra → água, as demais
            // levam terra da pilha de fora para a área de resíduos.
            _stepMark = 25;
            if (TryStartSoilFetch(ant) || TryStartRestock(ant) || TryStartWasteRestock(ant))
                return true;

            _stepMark = 26;
            return AntRoles.Harvests(role) && TryCutWork(ant);
        }

        /// <summary>
        /// Uma formiga emprestada ao trabalho da comum (<see cref="Ant.ActingAsWorker"/>) presa num cuidado contínuo
        /// (berçário, sala de nutrientes) larga-o quando o próprio papel volta a ter trabalho. Só cobre os papéis cujo
        /// trabalho dá para ver sem tentar: o soldado sai pelo combate; a carregadora volta quando há estoque a abastecer ou a
        /// entregar aos canteiros.
        /// </summary>
        bool RoleWorkWaiting(Ant ant)
        {
            if (!ant.ActingAsWorker)
                return false;
            switch (ant.Role)
            {
                case AntRole.Cutter:
                    return _gardenTarget >= 0 && !_harvestPaused && CutWanted && Work.IsEnabled(WorkSection.Harvest);
                case AntRole.Digger:
                    return _grid.MarkedCount > 0 && Work.IsEnabled(WorkSection.Dig) || _hasSite && Work.IsEnabled(WorkSection.Build);
                case AntRole.Nurse:
                    return _nursery.Blocks > 0 && _nursery.Specialists < Desired(_nursery.Blocks) && Work.IsEnabled(WorkSection.Nursery);
                case AntRole.Gardener:
                    return _garden.Blocks > 0 && _garden.Specialists < Desired(_garden.Blocks) && Work.IsEnabled(WorkSection.Garden);
                case AntRole.Carrier:
                    return LooseWanted > 0 || BlockRestockWanted || GardenSupplyOpen;
                case AntRole.Farmer:
                    return FarmWorkWaiting;
                default:
                    return false;
            }
        }

        void UpdateLoadedIdle(Ant ant)
        {
            if (ant.CarriedCount < LoadCapacity(ant) && ant.Carried != CellMaterial.Rock &&
                SeekWork(ant, _digField, rock: false, LoadedSeekRange))
                return;
            StartDelivery(ant);
        }

        // A formiga ociosa seguiu com o pedido aceito neste passo (UpdateAnt larga o de quem foi fazer outra coisa).
        bool _errandKept;

        /// <summary>
        /// Registra que a formiga atende <paramref name="errand"/>: na primeira vez, já conta para o pedido neste mesmo
        /// passo, para as formigas que decidem depois dela verem o pedido atendido.
        /// </summary>
        void TakeErrand(Ant ant, AntErrand errand)
        {
            _errandKept = true;
            if (ant.Errand == errand)
                return;
            ant.Errand = errand;
            if (errand == AntErrand.Cut)
                _cutters++;
            else if (errand == AntErrand.Build)
                _activeBuilders++;
        }

        /// <summary>Ainda falta planta que ninguém está cortando, levando ou deixou solta no chão.</summary>
        bool CutWanted => PlantSupplyDemand - _plantsInFlight - PlantsWaiting - _cutters > 0;

        /// <summary>Limite de passos do caminho até um pixel reservado; mais longe, a formiga segue o campo sem reservar.</summary>
        const int MaxSeekPath = 4096;
        // A formiga ociosa usou a reserva neste passo (UpdateAnt larga a reserva de quem foi fazer outra coisa).
        bool _seekKept;

        /// <summary>
        /// Trabalho do campo de escavação (<paramref name="rock"/> = campo de rocha) com reserva: ao sair para buscar, a
        /// formiga traça o caminho pelo campo até a célula de onde pega um pixel, reserva esse pixel (<see cref="Ant.SeekTarget"/>)
        /// e segue o caminho guardado. O pixel reservado sai dos campos na próxima rodada, então as outras vão a outro pixel
        /// livre (ou a outro trabalho) em vez de correrem todas para o mesmo e voltarem quando ele é pego.
        /// <paramref name="maxDistance"/> limita a distância no campo. Sem pixel livre no fim do caminho (outra formiga
        /// o reservou e o campo ainda não foi refeito), devolve <c>false</c> e a formiga segue para o próximo trabalho em vez
        /// de esperar ali (relato de 2026-10-19: dezenas de ociosas paradas disputando o mesmo pixel).
        /// <c>true</c> = a formiga agiu (andou ou começou a cavar).
        /// </summary>
        bool SeekWork(Ant ant, FlowField field, bool rock, int maxDistance)
        {
            if (ant.SeekTarget >= 0 && ant.SeekRock == rock)
            {
                if (ContinueSeek(ant))
                    return true;
                ant.ClearSeek();
            }

            int d = field.Get(ant.X, ant.Y);
            if (d == FlowField.Unreachable || d > maxDistance)
                return false;
            // Só a busca sem teto de distância desvia pela borda até outro ponto (a rainha e a formiga carregada ficam perto).
            bool nearby = maxDistance == FlowField.Unreachable;
            if (d == 0)
            {
                // Na borda sem pixel livre ao lado, procura outro ponto perto (PlanSeek) em vez de largar o trabalho.
                if (TryStartDig(ant, rock: rock))
                    return true;
                if (!nearby)
                    return false;
            }
            // A última busca chegou e não achou pixel livre: não refaz o caminho inteiro a cada passo.
            if (SimulationTime < ant.SeekRetryAt)
                return false;
            if (!PlanSeek(ant, field, rock, nearby, out bool tooFar))
            {
                return tooFar && StepDownhill(ant, field);
            }
            return ContinueSeek(ant);
        }

        // Quem reservou cada pixel de escavação neste passo (reserva de quem ainda anda; quem já cava trava em _claimed).
        // Fora de _claimed de propósito: o pixel reservado continua no campo de escavação, e quem chega primeiro o toma
        // de quem ainda está longe (troca de função, pedido de 2026-10-21). Com StealDigReservations desligado, não é usado.
        readonly Dictionary<int, Ant> _seekOwner = new Dictionary<int, Ant>();

        /// <summary>Guarda a reserva da formiga ociosa: de escavação de terra no mapa de donos; as demais travam a célula.</summary>
        void ReserveSeek(Ant ant)
        {
            if (ant.SeekRock || !_settings.StealDigReservations)
                Claim(ant.SeekTarget);
            else
                _seekOwner[ant.SeekTarget] = ant;
        }

        /// <summary>O pixel marcado está livre ou reservado por quem ainda está mais longe dele que <paramref name="arrival"/> passos.</summary>
        bool FreeOrTakeable(Ant ant, int x, int y, int arrival)
        {
            if (!_settings.StealDigReservations || _seekOwner.Count == 0)
                return true;
            if (!_seekOwner.TryGetValue(_grid.ToIndex(x, y), out Ant owner))
                return true;
            return !ReferenceEquals(owner, ant) && owner.State == AntState.Idle && owner.SeekTarget == _grid.ToIndex(x, y) &&
                   !owner.SeekArrived && owner.SeekRemaining > arrival;
        }

        /// <summary>Reserva o pixel para <paramref name="ant"/>; quem o tinha reservado larga a reserva e procura outro.</summary>
        void TakeSeek(Ant ant, int cell)
        {
            if (_seekOwner.TryGetValue(cell, out Ant owner) && !ReferenceEquals(owner, ant))
            {
                if (owner.State == AntState.Idle && owner.SeekTarget == cell)
                    owner.ClearSeek();
                owner.SeekRetryAt = 0f;
                // A reserva dele já estava nas contas do passo e a da formiga entra logo a seguir.
                if (IsDebrisAt(cell))
                    _debrisClaims--;
            }
            _seekOwner[cell] = ant;
        }

        /// <summary>
        /// Traça o caminho descendo o campo e, na célula de chegada, reserva um pixel livre vizinho. <c>false</c> sem pixel
        /// livre lá ou com caminho longo demais (<paramref name="tooFar"/>). Com <paramref name="nearby"/>, sem pixel livre na
        /// chegada, segue até o ponto mais perto dela que tenha um (<see cref="NearestSeekSpot"/>).
        /// </summary>
        bool PlanSeek(Ant ant, FlowField field, bool rock, bool nearby, out bool tooFar)
        {
            int[] dead = DeadCells(rock);
            _traceScratch.Clear();
            bool traced = TracePath(ant, field, out int x, out int y, out tooFar, dead, _traceScratch);
            if (!traced)
                return false;

            int count = CollectSeekCandidates(ant, x, y, rock, ant.SeekPathLength);
            if (count == 0)
            {
                // A descida do campo leva todas à mesma chegada, e o pixel reservado continua no campo (StealDigReservations):
                // sem pixel livre ali, segue pela borda até a célula mais perto que tenha (relato de 2026-10-21: ociosas
                // esperando a vez na mesma chegada com quase toda a face marcada livre).
                int spot = nearby ? NearestSeekSpot(ant, x, y, rock) : -1;
                if (spot < 0 || !AppendPathTo(ant, _grid.ToIndex(x, y), spot, SeekSpotLimit))
                {
                    // Sem pixel livre perto da chegada: as células do caminho levam todas a ela, e as reservas só crescem no passo.
                    foreach (int cell in _traceScratch)
                        dead[cell] = _seekTick;
                    ant.ClearSeek();
                    ant.SeekRetryAt = SimulationTime + SeekRetryDelay + (float)_rng.NextDouble() * SeekRetryDelay;
                    return false;
                }
                x = spot % _grid.Width;
                y = spot / _grid.Width;
                count = CollectSeekCandidates(ant, x, y, rock, ant.SeekPathLength);
            }
            int pick = _candidates[_rng.Next(count)];
            ant.SeekTarget = _grid.ToIndex(x + AntNavigation.Dx[pick], y + AntNavigation.Dy[pick]);
            ant.SeekRock = rock;
            if (rock || !_settings.StealDigReservations)
                Claim(ant.SeekTarget);
            else
                TakeSeek(ant, ant.SeekTarget);
            // Já ocupa a vaga neste passo: as que decidem depois dela não saem atrás de um pedaço que não cabe.
            if (!rock && IsDebrisAt(ant.SeekTarget))
                _debrisClaims++;
            return true;
        }

        /// <summary>Guarda em <c>_candidates</c> as direções de (<paramref name="x"/>, <paramref name="y"/>) com pixel que a formiga pode reservar chegando em <paramref name="arrival"/> passos.</summary>
        int CollectSeekCandidates(Ant ant, int x, int y, bool rock, int arrival)
        {
            int count = 0;
            for (int k = 0; k < 8; k++)
            {
                int tx = x + AntNavigation.Dx[k], ty = y + AntNavigation.Dy[k];
                if (rock ? IsRockTarget(tx, ty) : IsWorkTarget(tx, ty, x, y) && FitsLoad(ant, tx, ty) && FreeOrTakeable(ant, tx, ty, arrival))
                    _candidates[count++] = k;
            }
            return count;
        }

        /// <summary>Teto de células da busca local por outro ponto de escavação a partir da chegada (<see cref="NearestSeekSpot"/>).</summary>
        const int SeekSpotLimit = 512;

        /// <summary>
        /// Célula caminhável mais perto de (<paramref name="x"/>, <paramref name="y"/>), fora ela, com pixel que a formiga pode
        /// reservar; -1 = nenhuma até <see cref="SeekSpotLimit"/> células.
        /// </summary>
        int NearestSeekSpot(Ant ant, int x, int y, bool rock)
        {
            int w = _grid.Width;
            int start = y * w + x;
            int stamp = ++_floodCounter;
            int head = 0, tail = 0;
            _floodStamp[start] = stamp;
            _floodQueue[tail++] = start;
            int depth = 0, levelEnd = tail;
            while (head < tail)
            {
                if (head == levelEnd)
                {
                    depth++;
                    levelEnd = tail;
                }
                int i = _floodQueue[head++];
                int cx = i % w, cy = i / w;
                if (i != start && CollectSeekCandidates(ant, cx, cy, rock, ant.SeekPathLength + depth) > 0)
                    return i;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + AntNavigation.Dx[k], ny = cy + AntNavigation.Dy[k];
                    if (!_walkability.IsWalkable(nx, ny))
                        continue;
                    int n = ny * w + nx;
                    if (_floodStamp[n] == stamp || tail >= SeekSpotLimit)
                        continue;
                    _floodStamp[n] = stamp;
                    _floodQueue[tail++] = n;
                }
            }
            return -1;
        }

        /// <summary>
        /// Guarda na formiga o caminho descendo <paramref name="field"/> até a distância 0 e devolve a célula de chegada.
        /// <c>false</c> sem campo ou descida dali, ou com caminho longo demais (<paramref name="tooFar"/>).
        /// </summary>
        bool TracePath(Ant ant, FlowField field, out int x, out int y, out bool tooFar, int[] dead = null, List<int> visited = null)
        {
            tooFar = false;
            ant.BeginSeekPath();
            x = ant.X;
            y = ant.Y;
            int d = field.Get(x, y);
            if (d == FlowField.Unreachable)
                return false;
            int previous = ant.LastMove;
            while (d > 0)
            {
                if (ant.SeekPathLength >= MaxSeekPath)
                {
                    tooFar = true;
                    ant.ClearSeek();
                    return false;
                }
                int k = DownhillDirection(x, y, field, previous);
                if (k < 0)
                {
                    ant.ClearSeek();
                    return false;
                }
                previous = k;
                x += AntNavigation.Dx[k];
                y += AntNavigation.Dy[k];
                d = field.Get(x, y);
                int cell = _grid.ToIndex(x, y);
                // Caminho que já se sabe sem pixel livre na chegada (outra formiga acabou de tentá-lo neste passo).
                if (dead != null && dead[cell] == _seekTick)
                {
                    foreach (int seen in visited)
                        dead[seen] = _seekTick;
                    ant.ClearSeek();
                    return false;
                }
                ant.AddSeekStep(cell);
                visited?.Add(cell);
            }
            return true;
        }

        /// <summary>Passo atual da simulação, para validar <see cref="DeadCells"/> (zera os descartes sem limpar o vetor).</summary>
        int _seekTick = 1;        const float SeekRetryDelay = 0.4f;
        readonly List<int> _traceScratch = new List<int>();
        int[] _deadSoil, _deadRock;

        /// <summary>Células por onde a busca de escavação neste passo já sabe que não há pixel livre na chegada.</summary>
        int[] DeadCells(bool rock)
        {
            int size = _grid.Width * _grid.Height;
            if (rock)
                return _deadRock ??= new int[size];
            return _deadSoil ??= new int[size];
        }

        int[] _pathParent;

        /// <summary>
        /// Acrescenta ao caminho guardado da formiga o trecho mais curto de <paramref name="from"/> (o fim do caminho atual,
        /// ou a posição dela) até a célula <paramref name="target"/>, por busca em largura a partir do alvo. Serve às ordens
        /// com destino certo (a vaga reservada do depósito), que o campo compartilhado não dá; só para trechos curtos (até
        /// <paramref name="limit"/> células visitadas). <c>false</c> = sem caminho (o caminho guardado fica como estava).
        /// </summary>
        bool AppendPathTo(Ant ant, int from, int target, int limit)
        {
            int w = _grid.Width;
            int start = from;
            if (start == target)
                return true;
            if (!_walkability.IsWalkableAt(target) || !_walkability.IsWalkableAt(start))
                return false;
            _pathParent ??= new int[_grid.Width * _grid.Height];
            int stamp = ++_floodCounter;
            int head = 0, tail = 0;
            _floodStamp[target] = stamp;
            _pathParent[target] = -1;
            _floodQueue[tail++] = target;
            bool found = false;
            while (head < tail && !found)
            {
                int i = _floodQueue[head++];
                int x = i % w, y = i / w;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                    if (!_walkability.IsWalkable(nx, ny))
                        continue;
                    int n = ny * w + nx;
                    if (_floodStamp[n] == stamp)
                        continue;
                    _floodStamp[n] = stamp;
                    _pathParent[n] = i;
                    if (n == start)
                    {
                        found = true;
                        break;
                    }
                    if (tail >= limit)
                        return false;
                    _floodQueue[tail++] = n;
                }
            }
            if (!found)
                return false;
            for (int c = _pathParent[start]; c >= 0; c = _pathParent[c])
                ant.AddSeekStep(c);
            return true;
        }

        /// <summary>
        /// Um passo no caminho guardado (<see cref="TracePath"/>): 1 = andou, 0 = já está no fim, -1 = caminho cortado
        /// (a formiga caiu ou a próxima célula fechou).
        /// </summary>
        int StepPath(Ant ant)
        {
            if (ant.SeekArrived)
                return 0;
            int w = _grid.Width;
            int nx = ant.SeekNext % w, ny = ant.SeekNext / w;
            int dx = nx - ant.X, dy = ny - ant.Y;
            if (dx < -1 || dx > 1 || dy < -1 || dy > 1 || dx == 0 && dy == 0 || !AntNavigation.IsWalkable(_grid, nx, ny))
                return -1;
            ant.AdvanceSeek();
            MoveTo(ant, DirectionOf(dx, dy));
            return 1;
        }

        /// <summary>
        /// Um passo no caminho até o pixel reservado, ou começa a cavá-lo ao chegar. <c>false</c> se o pixel deixou de
        /// valer (caiu, foi desmarcado) ou o caminho foi cortado (a formiga caiu ou a célula fechou): aí ela traça de novo.
        /// </summary>
        bool ContinueSeek(Ant ant)
        {
            int w = _grid.Width;
            int tx = ant.SeekTarget % w, ty = ant.SeekTarget / w;
            if (!_grid.IsMarked(tx, ty))
                return false;
            CellMaterial material = _grid.Get(tx, ty);
            // A reserva desta formiga já está em DebrisRoom, por isso o pedaço solto vale com vaga zero.
            bool valid = ant.SeekRock
                ? material == CellMaterial.Rock
                : CellMaterials.IsDiggable(material) || CellMaterials.IsPlantDebris(material) && DebrisRoom >= 0;
            if (!valid || !ant.SeekRock && !FitsLoad(ant, tx, ty))
                return false;

            int nx, ny;
            if (ant.SeekArrived)
            {
                nx = tx;
                ny = ty;
            }
            else
            {
                nx = ant.SeekNext % w;
                ny = ant.SeekNext / w;
            }
            int dx = nx - ant.X, dy = ny - ant.Y;
            if (dx < -1 || dx > 1 || dy < -1 || dy > 1 || dx == 0 && dy == 0)
                return false;
            _seekKept = true;
            if (ant.SeekArrived)
            {
                BeginDig(ant, tx, ty, ant.SeekRock);
                return true;
            }
            if (!AntNavigation.IsWalkable(_grid, nx, ny))
            {
                _seekKept = false;
                return false;
            }
            ant.AdvanceSeek();
            MoveTo(ant, DirectionOf(dx, dy));
            return true;
        }

        static int DirectionOf(int dx, int dy)
        {
            for (int k = 0; k < 8; k++)
            {
                if (AntNavigation.Dx[k] == dx && AntNavigation.Dy[k] == dy)
                    return k;
            }
            return -1;
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
        /// Cortadeira ociosa: corta a planta-alvo. Sem alvo, com a sala de nutrientes cheia ou com o pedido de planta já
        /// atendido, não tem trabalho do papel (faz o da comum ou vaga pelo formigueiro).
        /// </summary>
        bool TryCutterWork(Ant ant) => _gardenTarget >= 0 && !_harvestPaused && TryCutWork(ant);

        /// <summary>
        /// Vai à linha de cima da planta-alvo pelo campo de corte e corta. Se todos os pixels da linha estão travados por
        /// outras, espera ali por perto. Só sai para cortar quem ainda falta: o pedido de planta já atendido por quem está
        /// indo, esperando ou cortando (<see cref="CutWanted"/>) não chama mais ninguém. <c>false</c> = não há o que cortar
        /// para esta formiga (sem alvo, sala cheia, pedido atendido ou sem caminho).
        /// </summary>
        bool TryCutWork(Ant ant)
        {
            if (_gardenTarget < 0 || _harvestPaused || !SectionAllows(WorkSection.Harvest, ant))
                return false;
            if (ant.Errand != AntErrand.Cut && !CutWanted)
                return false;
            int d = _gardenField.Get(ant.X, ant.Y);
            if (d == 0 && TryStartDig(ant, cutting: true))
            {
                TakeErrand(ant, AntErrand.Cut);
                return true;
            }
            if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, _gardenField))
            {
                TakeErrand(ant, AntErrand.Cut);
                return true;
            }
            if (d != FlowField.Unreachable || IsNearPlant(ant))
            {
                Hold(ant);
                TakeErrand(ant, AntErrand.Cut);
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
            if (cutting && !PlantDestinationReachable(ant.X, ant.Y))
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
            BeginDig(ant, ant.X + AntNavigation.Dx[pick], ant.Y + AntNavigation.Dy[pick], rock);
            return true;
        }

        /// <summary>Começa a cavar/cortar/quebrar o pixel vizinho (<paramref name="x"/>, <paramref name="y"/>) e o trava.</summary>
        void BeginDig(Ant ant, int x, int y, bool rock)
        {
            // O pedaço solto pego sem reserva (já ao lado) ocupa a vaga agora; o reservado já foi contado em PlanSeek.
            if (!rock && ant.SeekTarget != _grid.ToIndex(x, y) && CellMaterials.IsPlantDebris(_grid.Get(x, y)))
                _debrisClaims++;
            // Quem estava indo buscar este pixel perde a reserva (a formiga ao lado o cava antes dele chegar).
            if (!rock && _seekOwner.Count > 0 && _seekOwner.TryGetValue(_grid.ToIndex(x, y), out Ant displaced))
            {
                _seekOwner.Remove(_grid.ToIndex(x, y));
                if (!ReferenceEquals(displaced, ant))
                {
                    // Só a que ainda ia cavar o pixel: a que trocou de função aqui já segue outra ordem (água).
                    if (displaced.State == AntState.Idle && displaced.SeekTarget == _grid.ToIndex(x, y))
                        displaced.ClearSeek();
                    displaced.SeekRetryAt = 0f;
                    if (CellMaterials.IsPlantDebris(_grid.Get(x, y)))
                        _debrisClaims--;
                }
            }
            ant.ClearSeek();
            ant.DigX = x;
            ant.DigY = y;
            if (ant.DigX != ant.X)
                ant.FacingX = ant.DigX > ant.X ? 1 : -1;
            ant.State = AntState.Digging;
            ClaimTarget(ant);
            // A taxa de quebra da rocha (4 células/s) não muda com a velocidade de escavar.
            ant.Cooldown = rock
                ? _settings.RockBreakSeconds * Effects.RockBreakTime * _settings.DigTimeMultiplier
                : CellMaterials.DigSeconds(_grid.Get(ant.DigX, ant.DigY)) * _settings.DigTimeMultiplier * WorkTimeFactor(ant, _grid.Get(ant.DigX, ant.DigY));
            // Cada célula cavada gasta um uso da ferramenta (documento 45); planta e rocha não.
            if (!rock && !CellMaterials.IsAnyPlant(_grid.Get(ant.DigX, ant.DigY)))
                WearTool(ant);
        }

        /// <summary>Com a mão cheia pela metade, só vale o que é do mesmo tipo do que já se leva (terra com terra, planta com planta).</summary>
        bool FitsLoad(Ant ant, int x, int y)
        {
            if (ant.CarriedCount == 0)
                return true;
            CellMaterial target = _grid.Get(x, y);
            // Restos de cupim vão sozinhos; entulho só com entulho (os dois vão à área de resíduos, a terra não).
            if (ant.Carried == CellMaterial.TermiteRemains || ant.Carried == CellMaterial.Refuse || (ant.Carried == CellMaterial.Rubble) != (target == CellMaterial.Rubble))
                return false;
            // Com depósito de areia, areia só com areia: cada uma vai ao seu depósito. O cogumelo dos pulgões vai sozinho.
            if (SandDepot && IsSand(ant.Carried) != IsSand(target) || ant.Carried == CellMaterial.GlowMushroom ||
                ant.Carried == CellMaterial.Nectar || Equipment.IsMetalGood(ant.Carried))
                return false;
            // Com depósito de serapilheira, serapilheira só com serapilheira (REQ-070).
            if (LitterDepot && (ant.Carried == CellMaterial.Litter) != (target == CellMaterial.Litter))
                return false;
            // Minério só com minério do mesmo metal (documento 45).
            if ((CellMaterials.IsOre(ant.Carried) || CellMaterials.IsOre(target)) && ant.Carried != target)
                return false;
            return CellMaterials.IsPlant(ant.Carried) == CellMaterials.IsPlantDebris(target);
        }

        static bool IsSand(CellMaterial material) => material == CellMaterial.Sand || material == CellMaterial.PackedSand;

        /// <summary>Quantos pixels a formiga leva por viagem do que tem na mão (planta, terra ou rocha).</summary>
        int SoilCapacityOf(Ant ant) => Tree.SoilCapacity(ant.Role) + (ant.Role == AntRole.Digger ? Effects.DiggerSoilBonus : 0);

        int LoadCapacity(Ant ant)
        {
            if (CellMaterials.IsPlant(ant.Carried))
                return Tree.MaterialCapacity(ant.Role) + (ant.Role == AntRole.Carrier ? Effects.CarrierLoadBonus : 0);
            return ant.Carried == CellMaterial.Rock ? 1 : SoilCapacityOf(ant);
        }

        /// <summary>Segue com o que tem na mão: planta vai à sala de nutrientes (UpdateStoring); o resto, ao despejo.</summary>
        void StartDelivery(Ant ant)
        {
            ant.State = AntState.Carrying;
            ant.StoreTarget = StorageKind.None;
            // Planta vai ao depósito de alimentos ou, sem ele (ou cheio), aos canteiros (UpdateStoring). Entulho e restos de
            // cupim vão à área de resíduos, se houver lugar. Terra vai ao depósito de construção; sem ele, a um canteiro novo
            // (se houver ponto); senão ao despejo.
            if (CellMaterials.IsPlant(ant.Carried))
            {
                if (FoodDepot && !ant.FromStore)
                    TryStartStock(ant, StorageKind.Food);
                return;
            }
            // O cogumelo dos pulgões, o néctar das flores e o minério dos veios não são terra: vão ao depósito deles ou ficam no
            // chão como item solto (documentos 44 e 45).
            // Minério e serapilheira vão primeiro à fundição, se ela pede (documento 45).
            if (TryStartSmelterDelivery(ant))
                return;
            if (ant.Carried == CellMaterial.GlowMushroom || ant.Carried == CellMaterial.Nectar || CellMaterials.IsOre(ant.Carried) ||
                Equipment.IsMetalGood(ant.Carried))
            {
                ReturnHeldItem(ant);
                return;
            }
            if (TryStartWaste(ant))
                return;
            // Areia vai ao depósito de areia (a comida dos pulgões); sem ele, ao despejo como antes.
            if (IsSand(ant.Carried) && SandDepot && TryStartStock(ant, StorageKind.Sand))
                return;
            // Serapilheira, o insumo do bioma Serapilheira, vai ao depósito dela; sem ele (ou cheio), segue como terra.
            if (ant.Carried == CellMaterial.Litter && LitterDepot && TryStartStock(ant, StorageKind.Litter))
                return;
            if (BlockDepot ? IsBlockItem(ant.Carried) && TryStartStock(ant, StorageKind.Blocks) : TryStartBedSoil(ant))
                return;
            // Modo terra → água da área de resíduos: a terra que iria ao despejo vai para lá (com vaga para a areia).
            if (TryStartWaste(ant, dirt: true))
                return;
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
                Emit(SfxId.Dig, ant.DigX, ant.DigY);
                EmitFx(FxId.Dig, ant.DigX, ant.DigY, material);
                return;
            }

            // Pixel solto de planta volta a ser planta na mão e vai para a sala de nutrientes (UpdateStoring).
            ant.PickUp(debris ? CellMaterials.FromDebris(material) : CellMaterials.ToCarried(material), _grid.GetShade(ant.DigX, ant.DigY), _grid.ToIndex(ant.DigX, ant.DigY));
            _grid.Set(ant.DigX, ant.DigY, CellMaterial.Empty, 0);
            Emit(SfxId.Dig, ant.DigX, ant.DigY);
            EmitFx(FxId.Dig, ant.DigX, ant.DigY, ant.Carried);
            // Mão não cheia e mais um pixel do mesmo tipo ao alcance: continua antes de entregar.
            if (ant.CarriedCount >= LoadCapacity(ant))
            {
                StartDelivery(ant);
                return;
            }
            // Sem alvo encostado, fica ociosa com a carga: UpdateLoadedIdle procura um perto ou entrega.
            TryStartDig(ant);
        }

        /// <summary>
        /// Sem terra na mão: vai até a pilha de fora buscar um pixel (só se ainda faltar quem construa). Quem já está indo
        /// à pilha conta como construtora (<see cref="AntErrand.Build"/>), para a obra não chamar a colônia inteira.
        /// </summary>
        bool TryBuildWork(Ant ant)
        {
            if (!SectionAllows(WorkSection.Build, ant))
                return false;
            // Quem pega terra entra na fila mesmo sem ponto livre agora: espera perto da obra em vez de voltar ocioso.
            if (ant.Errand != AntErrand.Build && _activeBuilders >= BuildWorkCount)
                return false;
            // Com depósito de construção, a obra só usa o que está guardado nele.
            if (BlockDepot)
                return TryStartTake(ant, StoreUse.Build);
            int d = _pileField.Get(ant.X, ant.Y);
            if (d == FlowField.Unreachable)
                return false;
            if (d == 0)
                return TryStartGather(ant);
            if (!StepDownhill(ant, _pileField))
                return false;
            TakeErrand(ant, AntErrand.Build);
            return true;
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
            ant.Cooldown = CellMaterials.DigSeconds(_grid.Get(ant.DigX, ant.DigY)) * _settings.DigTimeMultiplier * WorkTimeFactor(ant, _grid.Get(ant.DigX, ant.DigY));
            // Quem chegou com o pedido de obra já foi contado ao aceitá-lo.
            if (!continuing && ant.Errand != AntErrand.Build)
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

            ant.PickUp(_grid.Get(ant.DigX, ant.DigY), _grid.GetShade(ant.DigX, ant.DigY), _grid.ToIndex(ant.DigX, ant.DigY));
            ant.ReturningSoil = false;
            _grid.Set(ant.DigX, ant.DigY, CellMaterial.Empty, 0);
            Emit(SfxId.Dig, ant.DigX, ant.DigY);
            EmitFx(FxId.Dig, ant.DigX, ant.DigY, ant.Carried);
            // Mão não cheia e mais terra solta ao alcance: pega mais antes de ir à obra.
            if (ant.CarriedCount < SoilCapacityOf(ant) && TryStartGather(ant, continuing: true))
                return;
            ant.State = AntState.Hauling;
        }

        void UpdateHauling(Ant ant)
        {
            if (ant.Carried == CellMaterial.GlowMushroom)
            {
                UpdateHaulingMushroom(ant);
                return;
            }
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
            // Sem obra: com depósito de construção, a terra volta para ele; senão, ao despejo.
            if (!(BlockDepot && IsBlockItem(ant.Carried) && TryStartStock(ant, StorageKind.Blocks)))
                AssignDumpSide(ant);
        }

        void Hold(Ant ant) => ant.Cooldown = 0.2f;

        bool TryStartPlace(Ant ant)
        {
            int count = 0;
            int antExit = _exitField.Get(ant.X, ant.Y);
            bool soil = ant.Carried != CellMaterial.GlowMushroom;
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (IsFillable(nx, ny, true) && (IsOutdoor(nx, ny) || antExit < _exitField.Get(nx, ny)) && !AntAt(nx, ny) &&
                    _grid.IsGlowMushroomMarked(nx, ny) == !soil)
                    _candidates[count++] = k;
                // Reboco de sala: a célula já é sólida, então não fecha passagem nem pede a ordem da obra.
                else if (soil && IsLiningOpen(nx, ny))
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
            if (!adjacent)
                return;
            // Sem marca de construção, o alvo era um slot de reboco de sala.
            if (!_grid.IsBuildMarked(ant.DigX, ant.DigY))
            {
                FinishLining(ant);
                return;
            }
            if (!IsFillable(ant.DigX, ant.DigY, false) || AntAt(ant.DigX, ant.DigY))
                return;

            // Terra solta voltaria a escorrer; encaixada, fica onde foi posta (e continua escavável). Na marca de cogumelo vai
            // o cogumelo colhido dos pulgões (pedido do desenvolvedor em 2026-10-22; antes um pixel de terra servia de substrato).
            bool mushroom = _grid.IsGlowMushroomMarked(ant.DigX, ant.DigY);
            if (mushroom != (ant.Carried == CellMaterial.GlowMushroom))
                return;
            _grid.Set(ant.DigX, ant.DigY, mushroom ? CellMaterial.GlowMushroom : CellMaterials.ToPacked(ant.Carried),
                      mushroom ? (byte)128 : ant.CarriedShade);
            if (mushroom)
                MushroomsPlanted++;
            // Marca como obra: do lado de fora a formiga anda por dentro dela (AntNavigation.IsOutdoorBuilt).
            _grid.SetBuilt(ant.DigX, ant.DigY, true);
            Emit(SfxId.Build, ant.DigX, ant.DigY);
            EmitFx(FxId.Build, ant.DigX, ant.DigY, ant.Carried);
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
            if (!TryDrop(ant) && !ClimbToDrop(ant))
                Wander(ant);
        }

        /// <summary>
        /// Sem vazio logo acima para soltar o grão (está dentro do monte ou sob uma planta): sobe pela face do bloco, dentro da
        /// faixa de despejo, até chegar ao topo. Antes do monte ser caminhável por dentro, ela ficava presa na borda.
        /// </summary>
        bool ClimbToDrop(Ant ant)
        {
            FlowField field = _dumpSides[ant.DumpSide].Field;
            // Reto para cima primeiro; depois as diagonais de cima (índices de AntNavigation.Dx/Dy).
            foreach (int k in ClimbDirections)
            {
                int nx = ant.X + AntNavigation.Dx[k];
                int ny = ant.Y + AntNavigation.Dy[k];
                if (field.Get(nx, ny) != 0 || !AntNavigation.IsWalkable(_grid, nx, ny))
                    continue;
                MoveTo(ant, k);
                return true;
            }
            return false;
        }

        static readonly int[] ClimbDirections = { 2, 4, 6 };

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
                Emit(SfxId.Drop, x, y);
                EmitFx(FxId.Drop, x, y, ant.Carried);
                ant.DropTop();
                // O pixel solto já conta no monte (DumpLoad): sai dos que estão a caminho.
                if (ant.DumpSide >= 0 && ant.DumpPixels > 0)
                {
                    ant.DumpPixels--;
                    _dumpSides[ant.DumpSide].InFlight--;
                }
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

        /// <summary>Anda para o vizinho caminhável de menor distância no campo (desempate em <see cref="AntNavigation.Downhill"/>).</summary>
        bool StepDownhill(Ant ant, FlowField field)
        {
            int chosen = DownhillDirection(ant.X, ant.Y, field, ant.LastMove);
            if (chosen < 0)
                return false;
            MoveTo(ant, chosen);
            return true;
        }

        /// <summary>Direção do vizinho caminhável de menor distância; -1 = nenhum desce (ver <see cref="AntNavigation.Downhill"/>).</summary>
        int DownhillDirection(int x, int y, FlowField field, int previous) =>
            AntNavigation.Downhill(_grid, field, _rng, x, y, previous);

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
            ant.LastMove = direction;
            if (dx != 0)
                ant.FacingX = dx;
            ant.Cooldown += step;
            if (ant.Cooldown < 0f)
                ant.Cooldown = 0f;
        }

        // A rainha anda como as formigas do papel dela (o da classe escolhida; no ninho, o da operária), desde 2026-10-21.
        float SpeedOf(Ant ant) =>
            ant.SpeedOverride > 0f ? ant.SpeedOverride
            : _settings.AntSpeed * AntRoles.SpeedFactor(ant.Role) * Tree.SpeedFactor(ant.Role)
              * (ant.Role == AntRole.Worker ? Effects.WorkerSpeedRate : ant.Role == AntRole.Carrier ? Effects.CarrierSpeedRate
                 : ant.Role == AntRole.Farmer ? Effects.FarmerSpeedRate : 1f)
              * SituationalSpeedRate(ant)
              * (_idleStroll ? _settings.IdleSpeedRate : 1f)
              * (ant.Y > _grid.GetSurfaceY(ant.X) ? Effects.OutdoorSpeedRate : Effects.TrailSpeedRate);

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
            /// <summary>Pixels a caminho desta faixa (na mão das formigas que a escolheram e ainda não os soltaram).</summary>
            public int InFlight;
            /// <summary>Origens do campo (o céu caminhável das colunas) e a <see cref="PixelGrid.StructureVersion"/> em que foram lidas.</summary>
            public readonly List<int> Sources = new List<int>();
            public int SourcesVersion = -1;

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
