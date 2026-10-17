using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    public enum AntState
    {
        Idle,
        Digging,
        Carrying,
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
        public CellMaterial Carried;
        public byte CarriedShade;

        // Último passo, para a visualização interpolar entre células (não afeta a simulação).
        public int FromX;
        public int FromY;
        public float MoveStart;
        public float MoveDuration;
    }
}
