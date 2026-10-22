namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Um pulgão da fazenda (pedido do desenvolvedor em 2026-10-22). Passeia pelo chão da fazenda e só vai à baia dele (os
    /// dois cochos, areia e água) para comer. O corpo (<see cref="Body"/>) é um <see cref="Ant"/> só para reaproveitar o
    /// passo, o apoio e o desenho das formigas; ele nunca entra em <see cref="AntColony.Ants"/>. Mordido por um cupim, deixa
    /// de existir aqui e vira um pulgão preto (um <see cref="Termite"/> com <see cref="Termite.Aphid"/>). Vai para o save
    /// (posição); os relógios e a fome recomeçam ao carregar.
    /// </summary>
    public sealed class Aphid
    {
        public readonly Ant Body = new Ant();
        /// <summary>Identidade estável (a fazendeira que o ordenha a guarda em <see cref="Ant.EggId"/>).</summary>
        public int Id;
        /// <summary>Baia dele: índice da célula do cocho de areia (o de água é a vizinha da direita); -1 = sem baia.</summary>
        public int Stall = -1;
        /// <summary>Já comeu (areia e água dos cochos) e pode ser ordenhado; a ordenha o deixa com fome de novo.</summary>
        public bool Fed;
        /// <summary>Segundos comendo nos cochos.</summary>
        public float EatTimer;
        /// <summary>Célula do chão da fazenda para onde está passeando; -1 = nenhuma (parado ou escolhendo).</summary>
        public int WanderTarget = -1;
        /// <summary>Segundos de ordenha acumulados para a próxima colheita.</summary>
        public float MilkTimer;
        /// <summary>Segundos sem ordenha acumulados para a próxima água.</summary>
        public float DripTimer;
        /// <summary>Fazendeira que o ordenha neste passo (recontada a cada Tick); null = ninguém.</summary>
        public Ant Farmer;

        public int X => Body.X;
        public int Y => Body.Y;
    }
}
