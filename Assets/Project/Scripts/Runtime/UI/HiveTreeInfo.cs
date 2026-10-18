using System.Collections.Generic;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Nós da árvore do formigueiro (cultivo, berçário, depósito e resíduos; seções 17 a 21 do design), só para exibição:
    /// os sistemas por trás (fungo, folhas, depósito e resíduos) ainda não existem no jogo, então nenhum nó pode ser
    /// comprado. Quando um sistema entrar, o nó passa a ter dono em <c>Colony/</c>, como a árvore da rainha.
    /// A ordem é a dos widgets montados em <c>AntFarmHudSetup</c>.
    /// </summary>
    public static class HiveTreeInfo
    {
        public sealed class Node
        {
            public string Id;
            public string Name;
            public string Effect;
            public int Cost;
            public string Requires;
            /// <summary>Nó pai de cada ligação desenhada (índices em <see cref="All"/>; -1 = o centro da colônia).</summary>
            public int[] Parents;
            /// <summary>Posição na referência <c>22-arvore-formigueiro-radial.png</c> (1920x1080, y para baixo).</summary>
            public float X, Y;
        }

        static Node N(string id, string name, string effect, int cost, string requires, float x, float y, params int[] parents) =>
            new Node { Id = id, Name = name, Effect = effect, Cost = cost, Requires = requires, X = x, Y = y, Parents = parents };

        public static readonly IReadOnlyList<Node> All = new[]
        {
            N("C0", "Cultivo inicial", "Sem produção → 0,25 UM/s por B; 3 N por UM de folha", 0, "Nenhum", 765, 487, -1),
            N("C1", "Canteiros organizados", "0,25 UM/s por B → 0,325 UM/s por B (+30%)", 90, "C0", 585, 380, 0),
            N("C2", "Área de preparo", "Folhas diretas → prepara 2 UM em 2 s; reserva de 12 UM por B", 180, "C1", 393, 275, 1),
            N("C3", "Cultivo produtivo", "3 N por UM de folha → 3,6 N (+20%); fruto 6 → 7,2 N", 200, "C1", 343, 477, 1),
            N("C4", "Cultivo integrado", "Redistribui a reserva preparada entre cultivos; alvo de 6 UM por B", 400, "C2 e C3", 168, 350, 2, 3),
            N("B0", "Berçário inicial", "Sem novas crias → 2 vagas de cria por B", 50, "C0", 1155, 487, -1),
            N("B1", "Leitos de criação", "Larva de 14 s → 10,5 s (−25%)", 110, "B0", 1335, 330, 5),
            N("B2", "Área de maturação", "Pupa de 12 s → 9 s (−25%)", 110, "B0", 1365, 540, 5),
            N("B3", "Berçário organizado", "2 vagas por B → 3 vagas por B (+50%)", 260, "B1 e B2", 1600, 408, 6, 7),
            N("D0", "Depósito inicial", "6 UM por B no cultivo → 20 UM por B no depósito", 80, "C0", 765, 702, -1),
            N("D1", "Pilhas organizadas", "20 UM por B → 30 UM por B (+50%)", 150, "D0", 573, 810, 9),
            N("D2", "Espaço para cargas grandes", "Cargas soltas de até 4 UM → guarda peças inteiras de até 8 UM", 260, "D1", 342, 905, 10),
            N("R0", "Área de resíduos", "Resíduos ocupam a origem → 12 UR por B; decomposição de 120 s por UR", 60, "C0", 1155, 702, -1),
            N("R1", "Decomposição acelerada", "120 s por UR → 60 s por UR (−50%)", 140, "R0", 1350, 860, 12),
        };

        /// <summary>Rótulos dos ramos e onde ficam (referência), para o desenho.</summary>
        public static readonly (string Label, float X, float Y)[] Branches =
        {
            ("CULTIVO", 765, 408), ("BERÇÁRIO", 1155, 416), ("DEPÓSITO", 765, 828), ("RESÍDUOS", 1155, 828),
        };
    }
}
