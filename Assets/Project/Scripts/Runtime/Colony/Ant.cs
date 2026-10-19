using System;
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
        /// <summary>Cuidando da sala de nutrientes: indo a um canteiro que brota ou parada ao lado dele.</summary>
        Gardening,
        /// <summary>Obsoleto: levava um pixel que curava para outro slot. Só aparece em saves antigos (volta ociosa).</summary>
        MovingNutrient,
        /// <summary>Levando terra fofa para montar um canteiro na sala de nutrientes. Valor novo: não renumerar.</summary>
        BringingSoil,
        /// <summary>Indo buscar um bloco de água no mapa para regar um canteiro com planta.</summary>
        FetchingWater,
        /// <summary>Levando o bloco de água até o canteiro com planta.</summary>
        CarryingWater,
        /// <summary>Indo à terra solta de fora (a pilha do despejo) buscar terra fofa para um canteiro.</summary>
        FetchingSoil,
        /// <summary>Pegando a terra fofa da pilha de fora (o alvo fica em DigX/DigY); depois leva ao canteiro.</summary>
        GatheringSoil,
        /// <summary>Ataque de cupins: largou o trabalho e vai morder o cupim mais perto (ou já está mordendo). Valor novo: não renumerar.</summary>
        Fighting,
        /// <summary>Desmembrando o corpo de um cupim (o alvo fica em TargetX/TargetY); termina com uma folha ou um resto na boca.</summary>
        Dismembering,
        /// <summary>Levando entulho ou restos de cupim até a área de resíduos.</summary>
        CarryingWaste,
        /// <summary>Jardineira cuidando da sala Gotas de Orvalho: vai até a queda das gotas e fica ao lado, acelerando-as. Valor novo: não renumerar.</summary>
        TendingDew,
    }

    /// <summary>
    /// Papel de gameplay da formiga (valores gravados no save: não renumerar). Quem corta plantas está em
    /// <see cref="AntRoles.Harvests"/>. A jardineira cuida da sala de nutrientes (o cultivo). O soldado
    /// só ataca: libera-se depois do primeiro ataque de cupins (nó S0 da árvore da rainha).
    /// </summary>
    public enum AntRole : byte
    {
        Worker = 0,
        Gardener = 1,
        Carrier = 2,
        Cutter = 3,
        Digger = 4,
        Nurse = 5,
        Soldier = 6,
    }

    /// <summary>Números por papel, vindos da seção 15 do design (velocidade e custo alimentar) e do que cada papel muda hoje no jogo.</summary>
    public static class AntRoles
    {
        /// <summary>Papéis que a rainha sabe criar, na ordem de exibição.</summary>
        public static readonly AntRole[] All =
            { AntRole.Worker, AntRole.Carrier, AntRole.Cutter, AntRole.Digger, AntRole.Gardener, AntRole.Nurse, AntRole.Soldier };

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

        /// <summary>Trabalha na sala Gotas de Orvalho (terra fofa, folha): a comum e a jardineira.</summary>
        public static bool WorksDew(AntRole role) => role == AntRole.Worker || role == AntRole.Gardener;

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

        /// <summary>Busca água no mapa para regar os canteiros: a comum, a jardineira (o cultivo é dela) e a carregadora.</summary>
        public static bool CarriesWater(AntRole role) => role == AntRole.Worker || role == AntRole.Gardener || role == AntRole.Carrier;

        /// <summary>Leva nutrientes prontos à rainha: a escavadora não (ela prioriza obras), nem o soldado.</summary>
        public static bool FetchesNutrients(AntRole role) => role != AntRole.Digger && role != AntRole.Nurse && role != AntRole.Soldier;

        /// <summary>Desmembra corpos de cupim e leva as partes (design 08/21: comuns retiram corpos), além do soldado.</summary>
        public static bool ClearsCorpses(AntRole role) => role == AntRole.Worker || role == AntRole.Carrier || role == AntRole.Soldier;

        /// <summary>Vida (pontos de dano) da tabela 15 do design: comum, cortadeira, jardineira e cuidadora 6; carregadora e escavadora 8; soldado 18.</summary>
        public static float BaseHealth(AntRole role)
        {
            switch (role)
            {
                case AntRole.Carrier:
                case AntRole.Digger: return 8f;
                case AntRole.Soldier: return 18f;
                default: return 6f;
            }
        }

        /// <summary>Dano por segundo da tabela 15 do design: 1 para as produtivas (autodefesa), 3 para o soldado.</summary>
        public static float BaseDamage(AntRole role) => role == AntRole.Soldier ? 3f : 1f;

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
                case AntRole.Soldier: return 34;
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
                case AntRole.Nurse:
                case AntRole.Soldier: return 10f;
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
                case AntRole.Soldier: return "Soldado";
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

    /// <summary>
    /// Como a rainha trata os cupins no formigueiro (pedido do desenvolvedor em 2026-10-18): <see cref="Defensive"/> só morde
    /// de volta quem acabou de mordê-la; <see cref="Offensive"/> morde qualquer cupim ao alcance.
    /// </summary>
    public enum QueenStance : byte
    {
        Defensive = 0,
        Offensive = 1,
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
        /// <summary>Pixels desta formiga contados em <c>InFlight</c> da faixa escolhida (saem um a um ao soltar).</summary>
        public int DumpPixels;
        /// <summary>Pixel na mão em <see cref="AntState.Carrying"/>: terra vai ao despejo; planta, à sala de nutrientes.</summary>
        public CellMaterial Carried;
        public byte CarriedShade;
        /// <summary>
        /// Célula (índice da grade) de onde saiu o pixel do topo, para o desenho mostrar o mesmo bloco que estava no
        /// terreno; -1 = sem origem conhecida. Só visual: não muda a simulação nem vai para o save.
        /// </summary>
        public int CarriedFrom = -1;

        // Pixels de baixo da pilha quando a formiga leva mais de um (o do topo está em Carried). O save guarda só o do topo.
        CellMaterial[] _below;
        byte[] _belowShade;
        int[] _belowFrom;
        int _belowCount;

        /// <summary>Pixels na mão (0 = nada). Todos do mesmo tipo: terra, planta ou rocha.</summary>
        public int CarriedCount => Carried == CellMaterial.Empty ? 0 : 1 + _belowCount;

        /// <summary>Pega mais um pixel; o que já estava na mão desce na pilha.</summary>
        /// <param name="from">Célula de origem (<see cref="CarriedFrom"/>), só para o desenho.</param>
        public void PickUp(CellMaterial material, byte shade, int from = -1)
        {
            if (Carried != CellMaterial.Empty)
            {
                if (_below == null)
                {
                    _below = new CellMaterial[16];
                    _belowShade = new byte[16];
                    _belowFrom = new int[16];
                }
                if (_belowCount < _below.Length)
                {
                    _below[_belowCount] = Carried;
                    _belowShade[_belowCount] = CarriedShade;
                    _belowFrom[_belowCount] = CarriedFrom;
                    _belowCount++;
                }
            }
            Carried = material;
            CarriedShade = shade;
            CarriedFrom = from;
        }

        /// <summary>Solta o pixel do topo; o próximo da pilha, se houver, passa a ser o do topo.</summary>
        public void DropTop()
        {
            if (_belowCount > 0)
            {
                _belowCount--;
                Carried = _below[_belowCount];
                CarriedShade = _belowShade[_belowCount];
                CarriedFrom = _belowFrom[_belowCount];
                return;
            }
            Carried = CellMaterial.Empty;
            CarriedShade = 0;
            CarriedFrom = -1;
        }

        /// <summary>Esvazia a mão (a carga se perde).</summary>
        public void ClearLoad()
        {
            _belowCount = 0;
            Carried = CellMaterial.Empty;
            CarriedShade = 0;
            CarriedFrom = -1;
        }
        /// <summary>Terra pegada para construir que está voltando ao despejo: não conta como entrega nova.</summary>
        public bool ReturningSoil;
        /// <summary>Ovo (<see cref="BroodRecord.Id"/>) que a formiga busca ou leva em <see cref="AntState.FetchingEgg"/>/<see cref="AntState.CarryingEgg"/>.</summary>
        public int EggId;
        /// <summary>Está tocando ou carregando o bloco que cuida (cria, canteiro que brota): só ele recebe o ritmo da especialista.</summary>
        public bool Interacting;
        /// <summary>Último bloco que mexeu (id da cria, ou célula+1 do canteiro), para o cuidado variar de bloco.</summary>
        public int LastBlock;
        /// <summary>Canteiro que a formiga cuida em <see cref="AntState.Gardening"/>.</summary>
        public int TargetX, TargetY;
        /// <summary>
        /// Pixel marcado que a formiga ociosa reservou e está indo pegar (índice da célula; -1 = nenhum). Enquanto a reserva
        /// vale, as outras não o escolhem e ele sai dos campos de escavação. Não vai para o save.
        /// </summary>
        public int SeekTarget = -1;
        /// <summary>A reserva em <see cref="SeekTarget"/> é de rocha (campo da escavadora), não de terra/planta solta.</summary>
        public bool SeekRock;

        // Caminho até a célula de onde se pega o pixel reservado (índices de célula, sem a posição de partida).
        int[] _seekPath;
        int _seekLength;
        int _seekStep;

        public int SeekPathLength => _seekLength;
        /// <summary>Já percorreu o caminho: está na célula de onde pega o pixel reservado.</summary>
        public bool SeekArrived => _seekStep >= _seekLength;
        /// <summary>Próxima célula do caminho (válido só sem <see cref="SeekArrived"/>).</summary>
        public int SeekNext => _seekPath[_seekStep];

        public void AdvanceSeek() => _seekStep++;

        /// <summary>Começa um caminho novo, ainda sem reserva.</summary>
        public void BeginSeekPath()
        {
            SeekTarget = -1;
            _seekLength = 0;
            _seekStep = 0;
        }

        public void AddSeekStep(int cell)
        {
            if (_seekPath == null)
                _seekPath = new int[32];
            else if (_seekLength == _seekPath.Length)
                Array.Resize(ref _seekPath, _seekLength * 2);
            _seekPath[_seekLength++] = cell;
        }

        /// <summary>Larga a reserva e o caminho.</summary>
        public void ClearSeek() => BeginSeekPath();

        /// <summary>Dano levado de cupins (0 = inteira). Ao chegar na vida do papel, morre. Recupera fora de combate; não vai para o save.</summary>
        public float Wounds;
        /// <summary>Tempo simulado da última mordida recebida (a recuperação só começa um tempo depois).</summary>
        public float LastHitTime = float.NegativeInfinity;
        /// <summary>Está mordendo um cupim neste momento (só para o desenho: anima como quem trabalha, virada para DigX/DigY).</summary>
        public bool Biting;
        /// <summary>Velocidade fixa em células/s que substitui a do papel (corpo dos cupins); 0 = a do papel.</summary>
        public float SpeedOverride;

        /// <summary>A terra fofa que a formiga leva (<see cref="AntState.BringingSoil"/>) é para a sala Gotas de Orvalho, não para um canteiro. Não vai para o save.</summary>
        public bool DewJob;

        /// <summary>Segundos seguidos com a célula da formiga debaixo d'água (zera ao sair). No limite, ela se afoga.</summary>
        public float UnderwaterSeconds;
        /// <summary>Segundos de jogo desde que a formiga nasceu. Ao chegar em <see cref="Lifespan"/>, ela morre de velhice.</summary>
        public float Age;
        /// <summary>Tempo de vida desta formiga em segundos (0 = não envelhece). Sorteado ao nascer em torno de <see cref="ColonySettings.AntLifespanSeconds"/>.</summary>
        public float Lifespan;

        // Último passo, para a visualização interpolar entre células (não afeta a simulação).
        public int FromX;
        public int FromY;
        public float MoveStart;
        public float MoveDuration;
    }
}
