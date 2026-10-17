using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    public enum AntState
    {
        Idle,
        Digging,
        Carrying,
        /// <summary>Pegando um pixel de terra da pilha de fora (modo construir). O alvo fica em DigX/DigY.</summary>
        Gathering,
        /// <summary>Levando a terra pegada até o ponto de construção.</summary>
        Hauling,
        /// <summary>Encaixando a terra numa célula marcada para construção. O alvo fica em DigX/DigY.</summary>
        Placing,
        /// <summary>Indo até a sala de nutrientes buscar um nutriente guardado para a rainha.</summary>
        Fetching,
        /// <summary>Levando um nutriente até a rainha (ela o consome ao receber).</summary>
        Delivering,
    }

    /// <summary>
    /// Papel de gameplay da formiga (valores gravados no save: não renumerar). Só a jardineira e a cortadeira colhem
    /// plantas; o resto da colônia não mexe nelas. Cuidadora e soldado ainda não existem (sem berçário nem invasoras).
    /// </summary>
    public enum AntRole : byte
    {
        Worker = 0,
        Gardener = 1,
        Carrier = 2,
        Cutter = 3,
        Digger = 4,
    }

    /// <summary>Números por papel, vindos da seção 15 do design (velocidade e custo alimentar) e do que cada papel muda hoje no jogo.</summary>
    public static class AntRoles
    {
        /// <summary>Papéis que a rainha sabe criar, na ordem de exibição.</summary>
        public static readonly AntRole[] All = { AntRole.Worker, AntRole.Carrier, AntRole.Cutter, AntRole.Digger, AntRole.Gardener };

        const float BaseSpeed = 24f; // células/s da operária comum no design

        /// <summary>Velocidade relativa à da comum (design: comum 24, carregadora 28, cortadeira 26, escavadora 24, jardineira 22).</summary>
        public static float SpeedFactor(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return 28f / BaseSpeed;
                case AntRole.Cutter: return 26f / BaseSpeed;
                case AntRole.Gardener: return 22f / BaseSpeed;
                default: return 1f;
            }
        }

        /// <summary>Multiplica o tempo de escavar e de pegar material para construir: a escavadora faz o dobro do trabalho por segundo (32 contra 16 células/s).</summary>
        public static float DigTimeFactor(AntRole role) => role == AntRole.Digger ? 0.5f : 1f;

        /// <summary>Multiplica o tempo de cortar planta: a cortadeira corta em 1 s o que a jardineira corta em 2 s.</summary>
        public static float CutTimeFactor(AntRole role) => role == AntRole.Cutter ? 0.5f : 1f;

        /// <summary>Colhe plantas da superfície (jardineira e cortadeira).</summary>
        public static bool Harvests(AntRole role) => role == AntRole.Gardener || role == AntRole.Cutter;

        /// <summary>Alimento por cria no design (N): comum 18, carregadora 24, cortadeira 24, escavadora 28, jardineira 26.</summary>
        public static int FoodCost(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier:
                case AntRole.Cutter: return 24;
                case AntRole.Digger: return 28;
                case AntRole.Gardener: return 26;
                default: return 18;
            }
        }

        /// <summary>Parte desejada de cada papel entre os disponíveis, do design (comuns 20, carregadoras 20, cortadeiras 10, escavadoras 15, jardineiras 15).</summary>
        public static float TargetWeight(AntRole role)
        {
            switch (role)
            {
                case AntRole.Cutter: return 10f;
                case AntRole.Digger:
                case AntRole.Gardener: return 15f;
                default: return 20f;
            }
        }

        public static string Label(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return "Carregadora";
                case AntRole.Cutter: return "Cortadeira";
                case AntRole.Digger: return "Escavadora";
                case AntRole.Gardener: return "Jardineira";
                default: return "Comum";
            }
        }
    }

    /// <summary>
    /// Política da rainha para as próximas formigas: <see cref="Automatic"/> segue a parte configurada de jardineiras,
    /// <see cref="Specific"/> cria sempre o papel escolhido e <see cref="Paused"/> não cria.
    /// </summary>
    public enum QueenMode : byte
    {
        Automatic = 0,
        Specific = 1,
        Paused = 2,
    }

    /// <summary>Estado de execução de uma formiga. Ocupa uma célula; o desenho pode ser maior.</summary>
    public sealed class Ant
    {
        public AntRole Role;
        public int X;
        public int Y;
        public int FacingX = 1;
        public AntState State;
        public float Cooldown;
        public int DigX;
        public int DigY;
        public int DumpTargetX;
        /// <summary>Faixa de despejo escolhida ao pegar o pixel (índice em <see cref="AntColony.DumpStrips"/>; -1 = nenhuma).</summary>
        public int DumpSide = -1;
        /// <summary>Pixel na mão em <see cref="AntState.Carrying"/>: terra vai ao despejo; planta, à sala de nutrientes.</summary>
        public CellMaterial Carried;
        public byte CarriedShade;
        /// <summary>Terra pegada para construir que está voltando ao despejo: não conta como entrega nova.</summary>
        public bool ReturningSoil;
        /// <summary>Segundos seguidos com a célula da formiga debaixo d'água (zera ao sair). No limite, ela se afoga.</summary>
        public float UnderwaterSeconds;

        // Último passo, para a visualização interpolar entre células (não afeta a simulação).
        public int FromX;
        public int FromY;
        public float MoveStart;
        public float MoveDuration;
    }
}
