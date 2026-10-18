using System.Collections.Generic;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Nós da árvore da rainha (seções 22 a 28 do design). A ordem é o formato do save (bit por nó em
    /// <see cref="QueenTree.Mask"/>): não reordenar nem remover; novos nós vão no fim.
    /// </summary>
    public enum QueenNode
    {
        Q0, Q1, Q2,
        O0, O1, O2, O3,
        L0, L1, L2, L3,
        F0, F1,
        E0, E1, E2,
        J0, J1, J2,
        N0, N1, N2,
        S0, S1, S2, S3,
    }

    /// <summary>Ramo (coluna) da árvore da rainha.</summary>
    public enum QueenBranch
    {
        Queen, Common, Carrier, Cutter, Digger, Gardener, Nurse, Soldier,
    }

    /// <summary>Definição de um nó: custo em nutrientes, pré-requisitos e o que ele muda (tudo vindo do design, custos são propostas).</summary>
    public sealed class QueenNodeDef
    {
        public QueenNode Node { get; }
        public QueenBranch Branch { get; }
        public string Name { get; }
        /// <summary>Efeito em uma frase: valor base e valor depois da compra, como na tabela do design.</summary>
        public string Effect { get; }
        public int Cost { get; }
        /// <summary>Todos exigidos ("exige X e Y").</summary>
        public IReadOnlyList<QueenNode> Requires { get; }
        /// <summary>
        /// False quando o sistema que o nó muda ainda não existe no jogo (carga por UM, cooperação, rocha, fungo, cuidadora,
        /// soldado…): o nó aparece, mas não pode ser comprado, para não haver bônus invisível.
        /// </summary>
        public bool Implemented { get; }
        /// <summary>Já vem adquirido (custo 0 e sem condição que o jogo ainda não cubra por outra regra).</summary>
        public bool Initial { get; }

        public string Id => Node.ToString();

        public QueenNodeDef(QueenNode node, QueenBranch branch, string name, string effect, int cost, bool implemented,
                            bool initial = false, params QueenNode[] requires)
        {
            Node = node;
            Branch = branch;
            Name = name;
            Effect = effect;
            Cost = cost;
            Implemented = implemented;
            Initial = initial;
            Requires = requires;
        }
    }

    /// <summary>
    /// Estado e regras da árvore da rainha: quais nós foram comprados, o que ainda pode ser comprado e os efeitos
    /// que eles têm nos números da colônia. Não conhece o saldo: <see cref="AntColony.TryBuy"/> debita os nutrientes.
    /// </summary>
    public sealed class QueenTree
    {
        public const float LayIntervalAfterQ1 = 12f / 18f;
        public const int EggsPerLayAfterQ2 = 3;

        static readonly QueenNodeDef[] Defs =
        {
            new QueenNodeDef(QueenNode.Q0, QueenBranch.Queen, "Postura inicial", "Sem ciclos antes do berçário → 1 ovo a cada 18 s", 0, true, initial: true),
            new QueenNodeDef(QueenNode.Q1, QueenBranch.Queen, "Postura acelerada", "18 s por ciclo → 12 s por ciclo (−33%)", 150, true, false, QueenNode.Q0),
            new QueenNodeDef(QueenNode.Q2, QueenBranch.Queen, "Postura em lotes", "1 ovo por ciclo → até 3 ovos por ciclo; cada um paga seu custo, lote parcial se faltar alimento ou vaga", 450, true, false, QueenNode.Q1),

            new QueenNodeDef(QueenNode.O0, QueenBranch.Common, "Operária comum", "Generalista: mantém todas as tarefas básicas", 0, true, initial: true),
            new QueenNodeDef(QueenNode.O1, QueenBranch.Common, "Carga ampliada", "Carga de 2 UM ou 32 células → 3 UM ou 48 (+50%) nas comuns", 100, true, false, QueenNode.O0),
            new QueenNodeDef(QueenNode.O2, QueenBranch.Common, "Deslocamento rápido", "24 células/s → 30 células/s (+25%) nas comuns", 100, true, false, QueenNode.O0),
            new QueenNodeDef(QueenNode.O3, QueenBranch.Common, "Retirada de companheiras", "Sem puxar adultas → resgata adulta a até 8 células da borda", 240, false, false, QueenNode.O0),

            new QueenNodeDef(QueenNode.L0, QueenBranch.Carrier, "Carregadora", "Libera a classe: 28 células/s, carga 4 UM", 140, true),
            new QueenNodeDef(QueenNode.L1, QueenBranch.Carrier, "Cargas maiores", "4 UM → 6 UM (+50%)", 160, true, false, QueenNode.L0),
            new QueenNodeDef(QueenNode.L2, QueenBranch.Carrier, "Transporte cooperativo", "Individual → 2 carregadoras levam peça de até 8 UM", 300, false, false, QueenNode.L1),
            new QueenNodeDef(QueenNode.L3, QueenBranch.Carrier, "Velocidade das carregadoras", "28 células/s → 35 células/s (+25%)", 200, true, false, QueenNode.L0),

            new QueenNodeDef(QueenNode.F0, QueenBranch.Cutter, "Cortadeira", "Libera a classe: corta 2 UM em 1 s (a comum leva 2 s)", 160, true),
            new QueenNodeDef(QueenNode.F1, QueenBranch.Cutter, "Cortes maiores", "2 UM em 1 s → 4 UM em 1,5 s", 220, true, false, QueenNode.F0),

            new QueenNodeDef(QueenNode.E0, QueenBranch.Digger, "Escavadora", "Libera a classe: escava 32 células/s (a comum faz 16)", 180, true),
            new QueenNodeDef(QueenNode.E1, QueenBranch.Digger, "Remoção ampliada", "32 células/s → 48 células/s (+50%) ao escavar", 200, true, false, QueenNode.E0),
            new QueenNodeDef(QueenNode.E2, QueenBranch.Digger, "Quebra de rocha", "Pedra bloqueia → quebra 4 células de rocha/s", 400, true, false, QueenNode.E1),

            new QueenNodeDef(QueenNode.J0, QueenBranch.Gardener, "Jardineira", "Libera a classe: cuida do cultivo (atende 6 B, a comum 4 B)", 180, true),
            new QueenNodeDef(QueenNode.J1, QueenBranch.Gardener, "Produção otimizada de fungo", "Crescimento (cura dos nutrientes) ×1 → ×1,3 na área atendida", 240, true, false, QueenNode.J0),
            new QueenNodeDef(QueenNode.J2, QueenBranch.Gardener, "Transplante de fungo", "Instalação de 8 s por B → 4 s por B", 300, false, false, QueenNode.J1),

            new QueenNodeDef(QueenNode.N0, QueenBranch.Nurse, "Cuidadora", "Libera a classe: atende 4 crias em 3 s", 200, true),
            new QueenNodeDef(QueenNode.N1, QueenBranch.Nurse, "Alimentação em lote", "4 crias por viagem → 8 crias por viagem", 240, true, false, QueenNode.N0),
            new QueenNodeDef(QueenNode.N2, QueenBranch.Nurse, "Resgate de crias", "Sem evacuação → transporta 2 crias por viagem, com prioridade de emergência", 320, false, false, QueenNode.N1),

            new QueenNodeDef(QueenNode.S0, QueenBranch.Soldier, "Soldado", "Libera a classe: dano 3/s, vida 18 (depois do primeiro ataque de cupins)", 220, true),
            new QueenNodeDef(QueenNode.S1, QueenBranch.Soldier, "Mandíbulas fortes", "Dano 3/s → 4,5/s (+50%)", 220, true, false, QueenNode.S0),
            new QueenNodeDef(QueenNode.S2, QueenBranch.Soldier, "Carapaça resistente", "Vida 18 → 27 (+50%)", 220, true, false, QueenNode.S0),
            new QueenNodeDef(QueenNode.S3, QueenBranch.Soldier, "Contenção", "Combate sem empurrão → empurra a invasora 4 células a cada 3 s", 500, false, false, QueenNode.S1, QueenNode.S2),
        };

        static readonly string[] BranchLabels =
        {
            "RAINHA", "COMUM", "CARREGADORA", "CORTADEIRA", "ESCAVADORA", "JARDINEIRA", "CUIDADORA", "SOLDADO",
        };

        /// <summary>Todos os nós, na ordem do enum (e do save).</summary>
        public static IReadOnlyList<QueenNodeDef> All => Defs;

        public const int BranchCount = 8;

        public static QueenNodeDef Def(QueenNode node) => Defs[(int)node];

        public static string BranchLabel(QueenBranch branch) => BranchLabels[(int)branch];

        /// <summary>Nós do ramo, na ordem de compra.</summary>
        public static List<QueenNodeDef> NodesOf(QueenBranch branch)
        {
            var list = new List<QueenNodeDef>();
            foreach (QueenNodeDef d in Defs)
            {
                if (d.Branch == branch)
                    list.Add(d);
            }
            return list;
        }

        /// <summary>Nós que um save anterior à árvore já tinha na prática: as classes que existiam, sem compra.</summary>
        public static ulong LegacyMask =>
            Bit(QueenNode.L0) | Bit(QueenNode.F0) | Bit(QueenNode.E0) | Bit(QueenNode.J0);

        static ulong Bit(QueenNode node) => 1UL << (int)node;

        ulong _mask;

        public QueenTree()
        {
            Restore(0);
        }

        /// <summary>Um bit por nó comprado (os iniciais sempre ligados). É o que vai no save.</summary>
        public ulong Mask => _mask;

        public bool Owns(QueenNode node) => (_mask & Bit(node)) != 0;

        /// <summary>
        /// O ramo do soldado só abre depois que a colônia sobreviveu ao primeiro ataque de cupins (a colônia liga isto; vai
        /// para o save junto com o estado dos ataques).
        /// </summary>
        public bool SoldiersAvailable { get; set; }

        /// <summary>Substitui o que foi comprado (os nós iniciais sempre contam; bits fora da tabela são descartados).</summary>
        public void Restore(ulong mask)
        {
            ulong valid = (1UL << Defs.Length) - 1;
            _mask = mask & valid;
            foreach (QueenNodeDef d in Defs)
            {
                if (d.Initial)
                    _mask |= Bit(d.Node);
            }
        }

        /// <summary>Compra todo nó que existe no jogo, ignorando custo e ordem (testes e cheats).</summary>
        public void UnlockAllImplemented()
        {
            foreach (QueenNodeDef d in Defs)
            {
                if (d.Implemented)
                    _mask |= Bit(d.Node);
            }
        }

        /// <summary>Por que o nó não pode ser comprado agora, ou <see cref="BuyBlock.None"/> se pode (o saldo é conferido pela colônia).</summary>
        public BuyBlock Check(QueenNode node)
        {
            QueenNodeDef d = Def(node);
            if (Owns(node))
                return BuyBlock.Owned;
            if (!d.Implemented)
                return BuyBlock.NotAvailableYet;
            if (d.Branch == QueenBranch.Soldier && !SoldiersAvailable)
                return BuyBlock.NeedsFirstRaid;
            foreach (QueenNode req in d.Requires)
            {
                if (!Owns(req))
                    return BuyBlock.MissingRequirement;
            }
            return BuyBlock.None;
        }

        /// <summary>Marca o nó como comprado se as regras permitirem (sem tocar no saldo). Devolve o motivo da recusa.</summary>
        public BuyBlock Grant(QueenNode node)
        {
            BuyBlock block = Check(node);
            if (block == BuyBlock.None)
                _mask |= Bit(node);
            return block;
        }

        // ---------------------------------------------------------------- efeitos

        /// <summary>A rainha já pode criar este papel (a comum sempre; as classes ao comprar o nó delas).</summary>
        public bool IsRoleUnlocked(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return Owns(QueenNode.L0);
                case AntRole.Cutter: return Owns(QueenNode.F0);
                case AntRole.Digger: return Owns(QueenNode.E0);
                case AntRole.Gardener: return Owns(QueenNode.J0);
                case AntRole.Nurse: return Owns(QueenNode.N0);
                case AntRole.Soldier: return Owns(QueenNode.S0);
                default: return true;
            }
        }

        /// <summary>Nó que libera o papel (nulo para a comum).</summary>
        public static QueenNode? UnlockNodeOf(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return QueenNode.L0;
                case AntRole.Cutter: return QueenNode.F0;
                case AntRole.Digger: return QueenNode.E0;
                case AntRole.Gardener: return QueenNode.J0;
                case AntRole.Nurse: return QueenNode.N0;
                case AntRole.Soldier: return QueenNode.S0;
                default: return null;
            }
        }

        /// <summary>Multiplica o intervalo de postura (Q1).</summary>
        public float LayIntervalFactor => Owns(QueenNode.Q1) ? LayIntervalAfterQ1 : 1f;

        /// <summary>Ovos por ciclo de postura (Q2).</summary>
        public int EggsPerLay => Owns(QueenNode.Q2) ? EggsPerLayAfterQ2 : 1;

        /// <summary>Velocidade relativa à base do papel (O2 nas comuns, L3 nas carregadoras), por cima de <see cref="AntRoles.SpeedFactor"/>.</summary>
        public float SpeedFactor(AntRole role)
        {
            if (role == AntRole.Worker && Owns(QueenNode.O2))
                return 30f / 24f;
            if (role == AntRole.Carrier && Owns(QueenNode.L3))
                return 35f / 28f;
            return 1f;
        }

        /// <summary>
        /// Pixels de planta (1 UM cada) que o papel leva por viagem: comum 2 (O1: 3), carregadora 4 (L1: 6), escavadora 4,
        /// as demais 2 (seção 15 do design, 1 pixel = 1 UM).
        /// </summary>
        public int MaterialCapacity(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return Owns(QueenNode.L1) ? 6 : 4;
                case AntRole.Digger: return 4;
                case AntRole.Worker: return Owns(QueenNode.O1) ? 3 : 2;
                default: return 2;
            }
        }

        /// <summary>
        /// Pixels de terra por viagem, no décimo da escala do design (32/64 células: o formigueiro do protótipo é cerca de
        /// dez vezes menor, como nas vagas): comum 3 (O1: 4), escavadora 6 (E1: 9), as demais 3.
        /// </summary>
        public int SoilCapacity(AntRole role)
        {
            if (role == AntRole.Digger)
                return Owns(QueenNode.E1) ? 9 : 6;
            if (role == AntRole.Worker && Owns(QueenNode.O1))
                return 4;
            return 3;
        }

        /// <summary>Crias que o papel atende ao mesmo tempo no berçário: comum 4, cuidadora 4 (N1: 8).</summary>
        public int CareCapacity(AntRole role) => role == AntRole.Nurse && Owns(QueenNode.N1) ? 8 : 4;

        /// <summary>Velocidade do cuidado em relação à comum (4 crias em 4 s): a cuidadora faz 4 em 3 s.</summary>
        public float CareRate(AntRole role) => role == AntRole.Nurse ? 4f / 3f : 1f;

        /// <summary>Blocos (B) de cultivo que o papel atende: comum 4, jardineira 6 (seção 15 do design); os outros não cuidam.</summary>
        public int GardenBlocks(AntRole role) => role == AntRole.Gardener ? 6 : role == AntRole.Worker ? 4 : 0;

        /// <summary>Ritmo da cura na área que o papel atende: ×1; a jardineira com J1, ×1,3.</summary>
        public float GardenRate(AntRole role) => role == AntRole.Gardener && Owns(QueenNode.J1) ? 1.3f : 1f;

        /// <summary>F1 da cortadeira: 4 UM em 1,5 s em vez de 2 UM em 1 s, ou seja, cada pixel leva 3/4 do tempo.</summary>
        public float CutTimeFactor(AntRole role) =>
            role == AntRole.Cutter && Owns(QueenNode.F1) ? 0.75f : 1f;

        /// <summary>Só a escavadora com E2 quebra rocha (a taxa não depende da velocidade de escavar).</summary>
        public bool CanBreakRock(AntRole role) => role == AntRole.Digger && Owns(QueenNode.E2);

        /// <summary>Vida máxima do papel (tabela 15 do design); o soldado com S2 passa de 18 para 27.</summary>
        public float MaxHealth(AntRole role) =>
            role == AntRole.Soldier && Owns(QueenNode.S2) ? 27f : AntRoles.BaseHealth(role);

        /// <summary>Dano por segundo do papel; o soldado com S1 passa de 3 para 4,5.</summary>
        public float Damage(AntRole role) =>
            role == AntRole.Soldier && Owns(QueenNode.S1) ? 4.5f : AntRoles.BaseDamage(role);

        /// <summary>Multiplicador do tempo de escavar e de pegar material: a escavadora passa de 32 para 48 células/s com E1 (E0 já vale em <see cref="AntRoles.DigTimeFactor"/>).</summary>
        public float DigTimeFactor(AntRole role) =>
            role == AntRole.Digger && Owns(QueenNode.E1) ? 2f / 3f : 1f;
    }

    /// <summary>Resultado da conferência de compra de um nó.</summary>
    public enum BuyBlock
    {
        None,
        Owned,
        MissingRequirement,
        /// <summary>O sistema que o nó muda ainda não existe no jogo.</summary>
        NotAvailableYet,
        InsufficientNutrients,
        /// <summary>O ramo do soldado espera o primeiro ataque de cupins.</summary>
        NeedsFirstRaid,
    }
}
