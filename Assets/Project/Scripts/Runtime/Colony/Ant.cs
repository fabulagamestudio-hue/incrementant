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
        /// <summary>Indo buscar o ovo que a rainha pôs (<see cref="Ant.EggId"/>). Valor novo: não renumerar os de cima (vão para o save).</summary>
        FetchingEgg,
        /// <summary>Levando o ovo na boca até um ponto livre do berçário.</summary>
        CarryingEgg,
        /// <summary>Cuidando do berçário: indo até uma cria ou parada ao lado dela (<see cref="Ant.EggId"/> = a cria). Valor novo: não renumerar.</summary>
        Tending,
        /// <summary>Levando uma cria na boca para outro ponto do berçário (<see cref="Ant.EggId"/> = a cria).</summary>
        MovingBrood,
        /// <summary>Cuidando da sala de nutrientes: indo a um pixel que cura ou parada ao lado dele.</summary>
        Gardening,
        /// <summary>Levando um pixel que cura (<see cref="Ant.CarriedCure"/> s restantes) para outro slot da sala.</summary>
        MovingNutrient,
    }

    /// <summary>
    /// Papel de gameplay da formiga (valores gravados no save: não renumerar). Quem corta plantas está em
    /// <see cref="AntRoles.Harvests"/>. A jardineira cuida da sala de nutrientes (o cultivo). O soldado
    /// ainda não existe (sem invasoras).
    /// </summary>
    public enum AntRole : byte
    {
        Worker = 0,
        Gardener = 1,
        Carrier = 2,
        Cutter = 3,
        Digger = 4,
        Nurse = 5,
    }

    /// <summary>Números por papel, vindos da seção 15 do design (velocidade e custo alimentar) e do que cada papel muda hoje no jogo.</summary>
    public static class AntRoles
    {
        /// <summary>Papéis que a rainha sabe criar, na ordem de exibição.</summary>
        public static readonly AntRole[] All = { AntRole.Worker, AntRole.Carrier, AntRole.Cutter, AntRole.Digger, AntRole.Gardener, AntRole.Nurse };

        const float BaseSpeed = 24f; // células/s da operária comum no design

        /// <summary>Velocidade relativa à da comum (design: comum 24, carregadora 28, cortadeira 26, escavadora 24, jardineira 22).</summary>
        public static float SpeedFactor(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier: return 28f / BaseSpeed;
                case AntRole.Cutter: return 26f / BaseSpeed;
                case AntRole.Gardener: return 22f / BaseSpeed;
                case AntRole.Nurse: return 26f / BaseSpeed;
                default: return 1f;
            }
        }

        /// <summary>Multiplica o tempo de escavar e de pegar material para construir: a escavadora faz o dobro do trabalho por segundo (32 contra 16 células/s).</summary>
        public static float DigTimeFactor(AntRole role) => role == AntRole.Digger ? 0.5f : 1f;

        /// <summary>Multiplica o tempo de cortar planta: a cortadeira corta em 1 s o que a comum corta em 2 s.</summary>
        public static float CutTimeFactor(AntRole role) => role == AntRole.Cutter ? 0.5f : 1f;

        /// <summary>Leva o ovo da rainha ao berçário: a operária comum e a carregadora (transportar é o trabalho dela).</summary>
        public static bool CarriesEggs(AntRole role) => role == AntRole.Worker || role == AntRole.Carrier || role == AntRole.Nurse;

        /// <summary>Cuida do berçário (design 14/19/26): a comum quando falta cuidado e a cuidadora, que o faz antes de tudo.</summary>
        public static bool Tends(AntRole role) => role == AntRole.Worker || role == AntRole.Nurse;

        /// <summary>A cuidadora vive no berçário: cuida antes de carregar ovo; a comum carrega o ovo primeiro.</summary>
        public static bool TendsFirst(AntRole role) => role == AntRole.Nurse;

        /// <summary>Cuida da sala de nutrientes (design 14/26): a comum quando falta cuidado e a jardineira, que o faz antes de tudo.</summary>
        public static bool Gardens(AntRole role) => role == AntRole.Worker || role == AntRole.Gardener;

        /// <summary>A jardineira vive na sala de nutrientes e só cuida dela (ou leva nutriente à rainha).</summary>
        public static bool GardensFirst(AntRole role) => role == AntRole.Gardener;

        /// <summary>
        /// Corta plantas da superfície: a cortadeira (dedicada) e, quando não têm outra tarefa, a comum e a carregadora
        /// (design 14/24: a carregadora corta sozinha; a comum corta, coleta e transporta). A jardineira cuida do cultivo de
        /// fungo (design 26), que ainda não existe, e a escavadora prioriza obras.
        /// </summary>
        public static bool Harvests(AntRole role) => role == AntRole.Worker || role == AntRole.Carrier || role == AntRole.Cutter;

        /// <summary>Só a cortadeira vive de cortar; as demais cortam apenas depois de buscar nutriente, cavar e construir.</summary>
        public static bool CutsFirst(AntRole role) => role == AntRole.Cutter;

        /// <summary>Leva nutrientes guardados à rainha: a escavadora não (ela prioriza obras).</summary>
        public static bool FetchesNutrients(AntRole role) => role != AntRole.Digger && role != AntRole.Nurse;

        /// <summary>Alimento por cria no design (N): comum 18, carregadora 24, cortadeira 24, escavadora 28, jardineira 26.</summary>
        public static int FoodCost(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier:
                case AntRole.Cutter: return 24;
                case AntRole.Digger: return 28;
                case AntRole.Gardener:
                case AntRole.Nurse: return 26;
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
                case AntRole.Nurse: return 10f;
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
                case AntRole.Nurse: return "Cuidadora";
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

        // Pixels de baixo da pilha quando a formiga leva mais de um (o do topo está em Carried). O save guarda só o do topo.
        CellMaterial[] _below;
        byte[] _belowShade;
        int _belowCount;

        /// <summary>Pixels na mão (0 = nada). Todos do mesmo tipo: terra, planta ou rocha.</summary>
        public int CarriedCount => Carried == CellMaterial.Empty ? 0 : 1 + _belowCount;

        /// <summary>Pega mais um pixel; o que já estava na mão desce na pilha.</summary>
        public void PickUp(CellMaterial material, byte shade)
        {
            if (Carried != CellMaterial.Empty)
            {
                if (_below == null)
                {
                    _below = new CellMaterial[16];
                    _belowShade = new byte[16];
                }
                if (_belowCount < _below.Length)
                {
                    _below[_belowCount] = Carried;
                    _belowShade[_belowCount] = CarriedShade;
                    _belowCount++;
                }
            }
            Carried = material;
            CarriedShade = shade;
        }

        /// <summary>Solta o pixel do topo; o próximo da pilha, se houver, passa a ser o do topo.</summary>
        public void DropTop()
        {
            if (_belowCount > 0)
            {
                _belowCount--;
                Carried = _below[_belowCount];
                CarriedShade = _belowShade[_belowCount];
                return;
            }
            Carried = CellMaterial.Empty;
            CarriedShade = 0;
        }

        /// <summary>Esvazia a mão (a carga se perde).</summary>
        public void ClearLoad()
        {
            _belowCount = 0;
            Carried = CellMaterial.Empty;
            CarriedShade = 0;
        }
        /// <summary>Terra pegada para construir que está voltando ao despejo: não conta como entrega nova.</summary>
        public bool ReturningSoil;
        /// <summary>Ovo (<see cref="BroodRecord.Id"/>) que a formiga busca ou leva em <see cref="AntState.FetchingEgg"/>/<see cref="AntState.CarryingEgg"/>.</summary>
        public int EggId;
        /// <summary>Está tocando ou carregando o bloco que cuida (cria, pixel que cura): só ele recebe o ritmo da especialista.</summary>
        public bool Interacting;
        /// <summary>Último bloco que mexeu (id da cria, ou célula+1 do pixel), para o cuidado variar de bloco.</summary>
        public int LastBlock;
        /// <summary>Slot de destino do pixel que a formiga muda de lugar em <see cref="AntState.MovingNutrient"/>.</summary>
        public int TargetX, TargetY;
        /// <summary>Segundos de cura que faltam ao pixel que a formiga muda de slot em <see cref="AntState.MovingNutrient"/>.</summary>
        public float CarriedCure;
        /// <summary>Segundos seguidos com a célula da formiga debaixo d'água (zera ao sair). No limite, ela se afoga.</summary>
        public float UnderwaterSeconds;

        // Último passo, para a visualização interpolar entre células (não afeta a simulação).
        public int FromX;
        public int FromY;
        public float MoveStart;
        public float MoveDuration;
    }
}
