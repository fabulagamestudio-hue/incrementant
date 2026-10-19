using Fabula.Incrementant.Settings;

namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Único ponto que lê o nível de efeitos visuais (<see cref="VfxLevel"/>). Todo efeito de juice pergunta aqui,
    /// nunca direto a <see cref="GameSettings"/>.
    /// </summary>
    public static class Vfx
    {
        public static VfxLevel Level => GameSettings.Vfx;

        /// <summary>Qualquer movimento ou partícula (Completo e Reduzido).</summary>
        public static bool Motion => Level != VfxLevel.Off;

        /// <summary>Efeitos fortes: tremidas, flashes, explosões (só Completo).</summary>
        public static bool Strong => Level == VfxLevel.Full;

        public static int Scale(int count) => Scale(Level, count);

        public static int Scale(VfxLevel level, int count)
        {
            switch (level)
            {
                case VfxLevel.Full: return count;
                case VfxLevel.Reduced: return (count + 1) / 2;
                default: return 0;
            }
        }
    }
}
