using System;
using System.Collections.Generic;
using Fabula.Incrementant.Colony;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>Aba da tela Evolução em que o nó aparece.</summary>
    public enum UpgradeTab
    {
        Hive,
        Queen,
    }

    /// <summary>Câmara = compra única; galeria = melhoria com nível (documento 39 do design).</summary>
    public enum UpgradeKind
    {
        Chamber,
        Gallery,
    }

    /// <summary>Tipo de galeria: decide o crescimento do custo, o teto e o marco de multiplicador.</summary>
    public enum GalleryType
    {
        Physical,
        Economy,
        Combat,
    }

    /// <summary>Condição do mundo exigida além dos nós (selo no nó).</summary>
    public enum UpgradeCondition
    {
        None,
        FirstRaid,
        FiveRaids,
        FiftyAdults,
    }

    /// <summary>Requisito: outro nó comprado (<see cref="Level"/> 0) ou uma galeria num nível mínimo.</summary>
    public readonly struct UpgradeRequirement
    {
        public readonly string Id;
        public readonly int Level;

        public UpgradeRequirement(string id, int level)
        {
            Id = id;
            Level = level;
        }

        public override string ToString() => Level > 0 ? $"{Id} Nv {Level}" : Id;
    }

    /// <summary>
    /// Um nó das duas abas da tela Evolução. Os números vêm de <c>Docs/04_Data/39_Melhorias_incrementais_valores.xlsx</c>
    /// (aprovados em 2026-10-19); textos ficam na tabela de localização (<see cref="Texts.Loc"/>).
    /// </summary>
    public sealed class UpgradeDef
    {
        public string Id { get; }
        public UpgradeTab Tab { get; }
        /// <summary>Ramo (direção do túnel) dentro da aba: índice em <see cref="UpgradeCatalog.Branches"/>.</summary>
        public int Branch { get; }
        /// <summary>0 = câmara central; 1 a 4 = anéis A1 a A4.</summary>
        public int Ring { get; }
        public UpgradeKind Kind { get; }
        /// <summary>Custo da câmara ou custo do nível 1 da galeria, em nutrientes.</summary>
        public int Cost { get; }
        public IReadOnlyList<UpgradeRequirement> Requires { get; }
        public UpgradeCondition Condition { get; }
        public GalleryType Type { get; }
        /// <summary>Nível máximo da galeria; 0 = sem teto.</summary>
        public int Cap { get; }
        /// <summary>Valor do atributo antes da galeria (já com as câmaras que o trocam).</summary>
        public float Base { get; }
        /// <summary>Efeito por nível: fração (modo %) ou unidade somada (modo absoluto).</summary>
        public float PerLevel { get; }
        public bool Additive { get; }
        /// <summary>Nó da árvore da rainha que já existe no jogo, ou null.</summary>
        public QueenNode? QueenNode { get; }
        /// <summary>Nó que já vem adquirido (centro, ou pincel que hoje é livre).</summary>
        public bool Initial { get; }
        /// <summary>Nó novo do documento 39 (não existe nas seções 17 a 28).</summary>
        public bool IsNew { get; }

        public bool IsCenter => Ring == 0;
        public bool Capped => Cap > 0;

        internal UpgradeDef(string id, UpgradeTab tab, int branch, int ring, UpgradeKind kind, int cost, UpgradeRequirement[] requires,
                            UpgradeCondition condition, GalleryType type, int cap, float baseValue, float perLevel, bool additive,
                            QueenNode? queenNode, bool initial, bool isNew)
        {
            Id = id;
            Tab = tab;
            Branch = branch;
            Ring = ring;
            Kind = kind;
            Cost = cost;
            Requires = requires;
            Condition = condition;
            Type = type;
            Cap = cap;
            Base = baseValue;
            PerLevel = perLevel;
            Additive = additive;
            QueenNode = queenNode;
            Initial = initial;
            IsNew = isNew;
        }
    }

    /// <summary>
    /// Tabela dos nós do documento 39 (câmaras e galerias das abas Formigueiro e Rainha e classes) e as fórmulas de
    /// custo, marco e profundidade. Só dados: a compra de verdade continua em <see cref="QueenTree"/> para os nós que já
    /// têm sistema; o resto aparece como EM BREVE até ganhar efeito.
    /// </summary>
    public static class UpgradeCatalog
    {
        // Parâmetros da planilha (aba Parametros).
        public const float GrowthPhysical = 1.35f;
        public const float GrowthEconomy = 1.15f;
        public const float GrowthCombat = 1.22f;
        public const int MilestoneInterval = 10;
        public const float MilestoneFactor = 1.5f;
        public const int QualitativeMilestone = 25;
        public const int PointsPerChamber = 1;
        public const int PointsPerLevel = 1;
        public const int PointsPerCompletedGallery = 3;
        static readonly int[] RingDepth = { 0, 0, 5, 20, 50 };

        /// <summary>Chave do texto do ramo, por aba (a ordem é a direção do túnel na tela).</summary>
        public static readonly string[][] Branches =
        {
            new[]
            {
                "tree.branch.hive.cultivation", "tree.branch.hive.nursery", "tree.branch.hive.storage", "tree.branch.hive.waste",
                "tree.branch.hive.dew", "tree.branch.hive.works", "tree.branch.hive.outside", "tree.branch.hive.defense",
            },
            new[]
            {
                "tree.branch.queen", "tree.branch.common", "tree.branch.carrier", "tree.branch.cutter",
                "tree.branch.digger", "tree.branch.gardener", "tree.branch.nurse", "tree.branch.soldier",
            },
        };

        const int Cultivo = 0, Bercario = 1, Deposito = 2, Residuos = 3, Orvalho = 4, Obra = 5, Exterior = 6, Defesa = 7;
        const int Rainha = 0, Comum = 1, Carregadora = 2, Cortadeira = 3, Escavadora = 4, Jardineira = 5, Cuidadora = 6, Soldado = 7;

        static readonly List<UpgradeDef> Defs = new List<UpgradeDef>();
        static readonly Dictionary<string, UpgradeDef> ById = new Dictionary<string, UpgradeDef>();

        public static IReadOnlyList<UpgradeDef> All => Defs;

        static UpgradeCatalog()
        {
            const UpgradeTab H = UpgradeTab.Hive, Q = UpgradeTab.Queen;

            // ---------------- aba Formigueiro: câmaras
            Center(H, "P0", Cultivo);
            Center(H, "T0", Obra);
            Chamber(H, "C0", Cultivo, 1, 0, "P0", initial: true);
            Chamber(H, "C1", Cultivo, 1, 90, "C0");
            Chamber(H, "C2", Cultivo, 2, 180, "C1");
            Chamber(H, "C3", Cultivo, 2, 200, "C1");
            Chamber(H, "C4", Cultivo, 3, 400, "C2,C3");
            Chamber(H, "C5", Cultivo, 4, 2500, "C4,J+1:10", isNew: true);
            Chamber(H, "B0", Bercario, 1, 50, "C0");
            Chamber(H, "B1", Bercario, 2, 110, "B0");
            Chamber(H, "B2", Bercario, 2, 110, "B0");
            Chamber(H, "B3", Bercario, 3, 260, "B1,B2");
            Chamber(H, "B4", Bercario, 4, 1800, "B3,Q2,B+1:5", isNew: true);
            Chamber(H, "D0", Deposito, 1, 80, "C0", initial: true);
            Chamber(H, "D3", Deposito, 1, 40, "T0", initial: true, isNew: true);
            Chamber(H, "D4", Deposito, 1, 40, "T0", initial: true, isNew: true);
            Chamber(H, "D1", Deposito, 2, 150, "D0");
            Chamber(H, "D2", Deposito, 3, 260, "D1,L1");
            Chamber(H, "D5", Deposito, 4, 1600, "D2,D3,D4,L2", isNew: true);
            Chamber(H, "R0", Residuos, 1, 0, "C0", initial: true);
            Chamber(H, "R1", Residuos, 2, 140, "R0");
            // Fora do documento 39 (por isso sem isNew nem a regra de requisito cruzado dele): pedido do desenvolvedor em
            // 2026-10-20 (KI-045), upgrade da conversão terra → água da área de resíduos.
            Chamber(H, "R2", Residuos, 3, 300, "R1");
            Chamber(H, "G0", Orvalho, 1, 0, "T0", initial: true, isNew: true);
            Chamber(H, "G1", Orvalho, 2, 300, "G0,C1", isNew: true);
            Chamber(H, "G2", Orvalho, 3, 900, "G+1:10,J+2:5", isNew: true);
            Chamber(H, "T1", Obra, 1, 120, "T0", isNew: true);
            Chamber(H, "T2", Obra, 3, 700, "T+1:5,O2", isNew: true);
            Chamber(H, "X0", Exterior, 1, 80, "T0", isNew: true);
            Chamber(H, "X1", Exterior, 3, 800, "X+1:10,J1", isNew: true);
            Chamber(H, "M0", Defesa, 2, 250, "T0", UpgradeCondition.FirstRaid, isNew: true);
            Chamber(H, "M1", Defesa, 3, 900, "M0,B3,N2", UpgradeCondition.FiveRaids, isNew: true);
            Chamber(H, "M2", Defesa, 2, 300, "M0,S0", isNew: true);
            // Fazenda de pulgões e depósitos de areia e de cogumelos (pedido do desenvolvedor em 2026-10-22, fora do documento
            // 39; custos assumidos). Os dois depósitos são pincéis livres, como os outros depósitos.
            Chamber(H, "D6", Deposito, 1, 40, "T0", initial: true);
            Chamber(H, "D7", Deposito, 1, 40, "T0", initial: true);
            Chamber(H, "A0", Exterior, 1, 150, "T0");
            Chamber(H, "A1", Exterior, 2, 300, "A0");

            // ---------------- aba Formigueiro: galerias
            Gallery(H, "H+1", Obra, 1, 60, "P0", GalleryType.Physical, 10, 24f, -0.04f);
            Gallery(H, "C+1", Cultivo, 1, 40, "C1", GalleryType.Economy, 0, 1f, 0.08f);
            Gallery(H, "C+2", Cultivo, 2, 120, "C3", GalleryType.Economy, 0, 1f, 0.05f);
            Gallery(H, "C+3", Cultivo, 2, 150, "C+1:5", GalleryType.Physical, 10, 30f, -0.04f);
            Gallery(H, "B+1", Bercario, 3, 250, "B3", GalleryType.Physical, 10, 14f, -0.03f);
            Gallery(H, "B+2", Bercario, 3, 250, "B3,H+1:3", GalleryType.Physical, 8, 16f, -0.05f);
            Gallery(H, "D+1", Deposito, 2, 120, "D1", GalleryType.Physical, 8, 1f, 0.25f);
            Gallery(H, "D+2", Deposito, 3, 250, "D1,R0", GalleryType.Physical, 10, 1f, -0.05f);
            Gallery(H, "G+1", Orvalho, 2, 90, "G0", GalleryType.Physical, 10, 8f, -0.05f);
            Gallery(H, "G+2", Orvalho, 2, 120, "G1", GalleryType.Physical, 10, 24f, 4f, additive: true);
            Gallery(H, "T+1", Obra, 1, 50, "T0", GalleryType.Physical, 15, 1f, -0.03f);
            Gallery(H, "T+2", Obra, 2, 150, "T1,E0", GalleryType.Physical, 10, 1f, -0.03f);
            Gallery(H, "X+1", Exterior, 2, 100, "X0", GalleryType.Economy, 0, 1f, 0.06f);
            Gallery(H, "X+2", Exterior, 2, 90, "X0", GalleryType.Physical, 10, 1f, 0.03f);
            Gallery(H, "X+3", Exterior, 2, 100, "X0,F0", GalleryType.Physical, 10, 1f, 0.05f);
            Gallery(H, "M+1", Defesa, 2, 150, "M0", GalleryType.Physical, 10, 0.8f, 0.10f);
            Gallery(H, "M+2", Defesa, 2, 120, "M0", GalleryType.Physical, 8, 8f, 1f, additive: true);
            // Fora do documento 39: área pintável de cada ambiente (KI-043, pedido do desenvolvedor em 2026-10-21). A base é a
            // da cena (ColonySettings, "Limites de pintura"); os níveis somam células.
            Gallery(H, "C+4", Cultivo, 1, 60, "C0", GalleryType.Physical, 10, 150f, 30f, additive: true);
            Gallery(H, "B+3", Bercario, 1, 60, "C0", GalleryType.Physical, 10, 200f, 40f, additive: true);
            Gallery(H, "D+3", Deposito, 1, 60, "D0", GalleryType.Physical, 10, 150f, 30f, additive: true);
            Gallery(H, "R+1", Residuos, 1, 60, "R0", GalleryType.Physical, 10, 120f, 24f, additive: true);
            Gallery(H, "G+3", Orvalho, 1, 60, "G0", GalleryType.Physical, 10, 100f, 20f, additive: true);
            // +1 sala não conectada por nível em todos os ambientes (base = ColonySettings.MaxPaintRooms).
            Gallery(H, "T+3", Obra, 1, 150, "T0", GalleryType.Physical, 3, 2f, 1f, additive: true);
            // Fazenda de pulgões: limite de pulgões (base = ColonySettings.AphidBaseCap), tempo da ordenha (base =
            // AphidMilkSeconds) e área pintável (base = FarmRoomCells).
            Gallery(H, "A+1", Exterior, 2, 120, "A0", GalleryType.Physical, 10, 2f, 1f, additive: true);
            Gallery(H, "A+2", Exterior, 2, 100, "A0", GalleryType.Physical, 10, 20f, -0.05f);
            Gallery(H, "A+3", Exterior, 1, 60, "A0", GalleryType.Physical, 10, 120f, 20f, additive: true);

            // ---------------- aba Rainha e classes: câmaras (custos dos nós existentes = QueenTree)
            Center(Q, "Q0", Rainha, Colony.QueenNode.Q0);
            Center(Q, "O0", Comum, Colony.QueenNode.O0);
            Queen("Q1", Rainha, 1, "Q0", Colony.QueenNode.Q1);
            Queen("Q2", Rainha, 2, "Q1,B0", Colony.QueenNode.Q2);
            Chamber(Q, "Q3", Rainha, 4, 2400, "Q2,B3,Q+1:10", isNew: true);
            Queen("O1", Comum, 1, "O0", Colony.QueenNode.O1);
            Queen("O2", Comum, 1, "O0", Colony.QueenNode.O2);
            Queen("O3", Comum, 1, "O0", Colony.QueenNode.O3);
            Chamber(Q, "O4", Comum, 4, 2000, "O+1:10,O+3:10", UpgradeCondition.FiftyAdults, isNew: true);
            Queen("L0", Carregadora, 1, "O0", Colony.QueenNode.L0);
            Queen("L1", Carregadora, 2, "L0", Colony.QueenNode.L1);
            Queen("L2", Carregadora, 3, "L1,D1", Colony.QueenNode.L2);
            Queen("L3", Carregadora, 2, "L0", Colony.QueenNode.L3);
            Chamber(Q, "L4", Carregadora, 4, 1800, "L2,D2", isNew: true);
            Queen("F0", Cortadeira, 1, "O0", Colony.QueenNode.F0);
            Queen("F1", Cortadeira, 2, "F0", Colony.QueenNode.F1);
            Chamber(Q, "F2", Cortadeira, 3, 600, "F1,X+1:5", isNew: true);
            Queen("E0", Escavadora, 1, "O0", Colony.QueenNode.E0);
            Queen("E1", Escavadora, 2, "E0", Colony.QueenNode.E1);
            Queen("E2", Escavadora, 3, "E1,T+2:3", Colony.QueenNode.E2);
            Chamber(Q, "E3", Escavadora, 4, 1800, "E+1:10,T2", isNew: true);
            Queen("J0", Jardineira, 1, "O0,C0", Colony.QueenNode.J0);
            Queen("J1", Jardineira, 2, "J0,C1", Colony.QueenNode.J1);
            Queen("J2", Jardineira, 3, "J1", Colony.QueenNode.J2);
            Chamber(Q, "J3", Jardineira, 4, 2200, "J+1:25,R1", isNew: true);
            Queen("N0", Cuidadora, 1, "O0,B0", Colony.QueenNode.N0);
            Queen("N1", Cuidadora, 2, "N0", Colony.QueenNode.N1);
            Queen("N2", Cuidadora, 3, "N1", Colony.QueenNode.N2);
            Chamber(Q, "N3", Cuidadora, 3, 700, "N1,Q+2:5", isNew: true);
            Queen("S0", Soldado, 2, "O0", Colony.QueenNode.S0, UpgradeCondition.FirstRaid);
            Queen("S1", Soldado, 2, "S0", Colony.QueenNode.S1);
            Queen("S2", Soldado, 2, "S0", Colony.QueenNode.S2);
            Queen("S3", Soldado, 3, "S1,S2", Colony.QueenNode.S3);
            Chamber(Q, "S4", Soldado, 4, 1500, "S3,M0", isNew: true);
            // Classes novas do pedido de 2026-10-21 (fora do documento 39): a cuspideira no ramo do soldado, a lixeira no da comum.
            Queen("K0", Soldado, 2, "S0", Colony.QueenNode.K0, UpgradeCondition.FirstRaid);
            Queen("W0", Comum, 2, "O0", Colony.QueenNode.W0, UpgradeCondition.FirstRaid);
            // Fazendeira (pedido de 2026-10-22), no ramo da jardineira: exige a fazenda (A0, portal para a outra aba).
            Queen("Z0", Jardineira, 1, "O0,A0", Colony.QueenNode.Z0);
            Queen("Z1", Jardineira, 2, "Z0", Colony.QueenNode.Z1);

            // ---------------- aba Rainha e classes: galerias
            Gallery(Q, "Q+1", Rainha, 2, 200, "Q1", GalleryType.Physical, 10, 12f, -0.03f);
            Gallery(Q, "Q+2", Rainha, 1, 150, "Q0,B0", GalleryType.Physical, 15, 1f, -0.02f);
            Gallery(Q, "Q+3", Rainha, 1, 250, "Q0", GalleryType.Physical, 10, 60f, 0.10f, condition: UpgradeCondition.FirstRaid);
            Gallery(Q, "Q+4", Rainha, 1, 60, "Q0", GalleryType.Physical, 10, 200f, 40f, additive: true); // KI-043, ver C+4
            Gallery(Q, "O+1", Comum, 2, 80, "O2", GalleryType.Physical, 10, 30f, 0.04f);
            Gallery(Q, "O+2", Comum, 1, 150, "O0,B1", GalleryType.Physical, 10, 1f, 0.10f);
            Gallery(Q, "O+3", Comum, 2, 80, "O1", GalleryType.Physical, 10, 1f, -0.03f);
            Gallery(Q, "L+1", Carregadora, 2, 200, "L1", GalleryType.Physical, 4, 6f, 1f, additive: true);
            Gallery(Q, "L+2", Carregadora, 2, 120, "L3", GalleryType.Physical, 10, 35f, 0.04f);
            Gallery(Q, "L+3", Carregadora, 2, 140, "L3", GalleryType.Physical, 10, 1f, 0.04f);
            Gallery(Q, "F+1", Cortadeira, 2, 120, "F1", GalleryType.Physical, 10, 1f, -0.04f);
            Gallery(Q, "E+1", Escavadora, 2, 120, "E1", GalleryType.Physical, 10, 1f, -0.04f);
            Gallery(Q, "E+2", Escavadora, 2, 150, "E1", GalleryType.Physical, 6, 9f, 1f, additive: true);
            Gallery(Q, "E+3", Escavadora, 3, 300, "E2", GalleryType.Physical, 8, 1f, -0.06f);
            Gallery(Q, "J+1", Jardineira, 2, 150, "J1", GalleryType.Economy, 0, 1f, 0.05f);
            Gallery(Q, "J+2", Jardineira, 2, 120, "J0,G0", GalleryType.Physical, 10, 1f, 0.10f);
            Gallery(Q, "N+1", Cuidadora, 2, 120, "N1", GalleryType.Physical, 10, 1f, -0.05f);
            Gallery(Q, "N+2", Cuidadora, 2, 250, "N1", GalleryType.Physical, 4, 2f, 1f, additive: true);
            Gallery(Q, "S+1", Soldado, 3, 200, "S1", GalleryType.Combat, 0, 4.5f, 0.08f);
            Gallery(Q, "S+2", Soldado, 3, 200, "S2", GalleryType.Combat, 0, 27f, 0.08f);
            Gallery(Q, "S+3", Soldado, 3, 150, "S0,N0", GalleryType.Physical, 10, 10f, -0.05f);
            Gallery(Q, "S+4", Soldado, 3, 160, "S0", GalleryType.Physical, 10, 1f, 0.05f);
            Gallery(Q, "Z+1", Jardineira, 2, 100, "Z0", GalleryType.Physical, 10, 22f, 0.04f);
        }

        // ---------------------------------------------------------------- consulta

        public static UpgradeDef Find(string id) => id != null && ById.TryGetValue(id, out UpgradeDef def) ? def : null;

        public static UpgradeDef Get(string id) =>
            Find(id) ?? throw new ArgumentException($"Nó de evolução desconhecido: '{id}'.", nameof(id));

        /// <summary>Nós da aba, na ordem da tabela (é a ordem dos widgets montados pelo setup).</summary>
        public static List<UpgradeDef> OfTab(UpgradeTab tab)
        {
            var list = new List<UpgradeDef>();
            foreach (UpgradeDef def in Defs)
                if (def.Tab == tab)
                    list.Add(def);
            return list;
        }

        /// <summary>O nó do catálogo que corresponde a um nó da árvore da rainha.</summary>
        public static UpgradeDef Of(QueenNode node) => Get(node.ToString());

        /// <summary>Profundidade exigida para o anel abrir (0 no centro e em A1).</summary>
        public static int DepthToOpen(int ring) => RingDepth[Math.Max(0, Math.Min(ring, RingDepth.Length - 1))];

        // ---------------------------------------------------------------- fórmulas (documento 39, "Fórmulas")

        public static float Growth(GalleryType type) =>
            type == GalleryType.Physical ? GrowthPhysical : type == GalleryType.Economy ? GrowthEconomy : GrowthCombat;

        /// <summary>Custo do nível <paramref name="level"/> (o próximo a comprar, a partir de 1): custo base × r^(n−1).</summary>
        public static int LevelCost(UpgradeDef def, int level)
        {
            if (def.Kind == UpgradeKind.Chamber)
                return def.Cost;
            if (level < 1 || (def.Capped && level > def.Cap))
                return 0;
            return (int)Math.Round(def.Cost * Math.Pow(Growth(def.Type), level - 1), MidpointRounding.AwayFromZero);
        }

        /// <summary>Custo de comprar <paramref name="count"/> níveis a partir do nível atual (para no teto).</summary>
        public static long CostOfLevels(UpgradeDef def, int currentLevel, int count, out int bought)
        {
            long total = 0;
            bought = 0;
            for (int n = currentLevel + 1; bought < count && (!def.Capped || n <= def.Cap); n++, bought++)
                total += LevelCost(def, n);
            return total;
        }

        /// <summary>Quantos níveis o saldo paga a partir do nível atual (botão MÁX), e o custo deles.</summary>
        public static long MaxAffordable(UpgradeDef def, int currentLevel, long budget, out int bought)
        {
            long total = 0;
            bought = 0;
            for (int n = currentLevel + 1; !def.Capped || n <= def.Cap; n++)
            {
                int cost = LevelCost(def, n);
                if (total + cost > budget)
                    break;
                total += cost;
                bought++;
            }
            return total;
        }

        /// <summary>Multiplicador do marco: 1,5 a cada 10 níveis nas galerias de economia e combate; 1 nas físicas.</summary>
        public static float MilestoneMultiplier(UpgradeDef def, int level) =>
            def.Type == GalleryType.Physical ? 1f : (float)Math.Pow(MilestoneFactor, level / MilestoneInterval);

        /// <summary>Valor do atributo no nível dado (modo %: base × (1 + efeito × n × marco); absoluto: base + efeito × n).</summary>
        public static float ValueAt(UpgradeDef def, int level)
        {
            if (def.Capped)
                level = Math.Min(level, def.Cap);
            return def.Additive ? def.Base + def.PerLevel * level : def.Base * (1f + def.PerLevel * level * MilestoneMultiplier(def, level));
        }

        /// <summary>Próximo nível com marco (multiplicador ou o qualitativo do nível 25), ou 0 se a galeria não tem marco.</summary>
        public static int NextMilestone(UpgradeDef def, int level)
        {
            if (def.Kind != UpgradeKind.Gallery || def.Type == GalleryType.Physical)
                return 0;
            int next = (level / MilestoneInterval + 1) * MilestoneInterval;
            return level < QualitativeMilestone && QualitativeMilestone < next ? QualitativeMilestone : next;
        }

        // ---------------------------------------------------------------- montagem da tabela

        static void Center(UpgradeTab tab, string id, int branch, QueenNode? queen = null) =>
            Add(new UpgradeDef(id, tab, branch, 0, UpgradeKind.Chamber, 0, Array.Empty<UpgradeRequirement>(), UpgradeCondition.None,
                               GalleryType.Physical, 0, 0, 0, false, queen, initial: true, isNew: false));

        static void Chamber(UpgradeTab tab, string id, int branch, int ring, int cost, string requires,
                            UpgradeCondition condition = UpgradeCondition.None, bool initial = false, bool isNew = false) =>
            Add(new UpgradeDef(id, tab, branch, ring, UpgradeKind.Chamber, cost, Parse(requires), condition,
                               GalleryType.Physical, 0, 0, 0, false, null, initial, isNew));

        static void Queen(string id, int branch, int ring, string requires, QueenNode node,
                          UpgradeCondition condition = UpgradeCondition.None)
        {
            QueenNodeDef queen = QueenTree.Def(node);
            Add(new UpgradeDef(id, UpgradeTab.Queen, branch, ring, UpgradeKind.Chamber, queen.Cost, Parse(requires), condition,
                               GalleryType.Physical, 0, 0, 0, false, node, queen.Initial, isNew: false));
        }

        static void Gallery(UpgradeTab tab, string id, int branch, int ring, int cost, string requires, GalleryType type, int cap,
                            float baseValue, float perLevel, bool additive = false, UpgradeCondition condition = UpgradeCondition.None) =>
            Add(new UpgradeDef(id, tab, branch, ring, UpgradeKind.Gallery, cost, Parse(requires), condition, type, cap, baseValue,
                               perLevel, additive, null, initial: false, isNew: true));

        static void Add(UpgradeDef def)
        {
            Defs.Add(def);
            ById.Add(def.Id, def);
        }

        /// <summary>"C4,J+1:10" → C4 comprado e J+1 no nível 10.</summary>
        static UpgradeRequirement[] Parse(string requires)
        {
            string[] parts = requires.Split(',');
            var result = new UpgradeRequirement[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string[] pair = parts[i].Trim().Split(':');
                result[i] = new UpgradeRequirement(pair[0], pair.Length > 1 ? int.Parse(pair[1]) : 0);
            }
            return result;
        }
    }
}
