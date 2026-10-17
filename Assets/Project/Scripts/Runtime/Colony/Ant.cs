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

    /// <summary>Estado de execução de uma formiga. Ocupa uma célula; o desenho pode ser maior.</summary>
    public sealed class Ant
    {
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
