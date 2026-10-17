using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>Material de uma célula da grade. O valor é serializado em byte; não reordene.</summary>
    public enum CellMaterial : byte
    {
        Empty = 0,
        Dirt = 1,
        Grass = 2,
        Sand = 3,
        Rock = 4,
        Spoil = 5,
        Bedrock = 6,
        /// <summary>Terra despejada que a chuva forte compactou: não cai mais, mas continua escavável.</summary>
        PackedSpoil = 7,
        /// <summary>Areia que a chuva forte compactou.</summary>
        PackedSand = 8,
    }

    /// <summary>Regras e cores por material. Tabela fixa enquanto o protótipo não pede dados configuráveis.</summary>
    public static class CellMaterials
    {
        public static bool IsSolid(CellMaterial material) => material != CellMaterial.Empty;

        /// <summary>Materiais que caem e escorregam em diagonal (formam pilhas).</summary>
        public static bool IsGranular(CellMaterial material) =>
            material == CellMaterial.Sand || material == CellMaterial.Spoil;

        /// <summary>No que um material granular vira quando a chuva forte o compacta (os demais não mudam).</summary>
        public static CellMaterial ToPacked(CellMaterial material)
        {
            switch (material)
            {
                case CellMaterial.Sand: return CellMaterial.PackedSand;
                case CellMaterial.Spoil: return CellMaterial.PackedSpoil;
                default: return material;
            }
        }

        public static bool IsDiggable(CellMaterial material) =>
            material == CellMaterial.Dirt || material == CellMaterial.Grass ||
            material == CellMaterial.Sand || material == CellMaterial.Spoil ||
            material == CellMaterial.PackedSpoil || material == CellMaterial.PackedSand;

        /// <summary>Segundos que uma formiga leva para soltar um pixel deste material.</summary>
        public static float DigSeconds(CellMaterial material)
        {
            switch (material)
            {
                case CellMaterial.Sand: return 0.08f;
                case CellMaterial.Spoil: return 0.12f;
                case CellMaterial.Grass: return 0.35f;
                case CellMaterial.Dirt: return 0.25f;
                case CellMaterial.PackedSpoil: return 0.3f;
                case CellMaterial.PackedSand: return 0.2f;
                default: return float.PositiveInfinity;
            }
        }

        /// <summary>No que um pixel carregado vira ao ser despejado fora do formigueiro (o que foi compactado volta a soltar).</summary>
        public static CellMaterial ToDumped(CellMaterial carried) =>
            carried == CellMaterial.Sand || carried == CellMaterial.PackedSand ? CellMaterial.Sand : CellMaterial.Spoil;

        public static Color32 BaseColor(CellMaterial material)
        {
            switch (material)
            {
                case CellMaterial.Dirt: return new Color32(118, 82, 52, 255);
                case CellMaterial.Grass: return new Color32(86, 122, 52, 255);
                case CellMaterial.Sand: return new Color32(212, 186, 120, 255);
                case CellMaterial.Rock: return new Color32(112, 110, 118, 255);
                case CellMaterial.Spoil: return new Color32(156, 112, 70, 255);
                case CellMaterial.Bedrock: return new Color32(46, 40, 40, 255);
                case CellMaterial.PackedSpoil: return new Color32(124, 100, 84, 255);
                case CellMaterial.PackedSand: return new Color32(176, 150, 104, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }

        /// <summary>Cor final com a variação de tom da célula (0..255, 128 = neutro).</summary>
        public static Color32 ShadedColor(CellMaterial material, byte shade)
        {
            Color32 c = BaseColor(material);
            float f = 0.82f + shade * (0.36f / 255f);
            return new Color32(Scale(c.r, f), Scale(c.g, f), Scale(c.b, f), 255);
        }

        static byte Scale(byte value, float factor)
        {
            int v = (int)(value * factor);
            return (byte)(v > 255 ? 255 : v);
        }
    }
}
