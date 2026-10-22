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
        /// <summary>Terra mole: terra fixa (natural ou construída) que ficou tempo demais encostada na água. Granular.</summary>
        Mud = 9,
        /// <summary>Folha de grama que cresce na superfície (<see cref="VegetationSimulation"/>). Colhida, vira nutriente.</summary>
        PlantGrass = 10,
        /// <summary>Caule e folhas de uma muda folhosa da superfície. Colhida, vira nutriente.</summary>
        PlantLeaf = 11,
        /// <summary>Pixel de grama que a formiga soltou da planta: cai como grão e, no chão, espera ser colhido.</summary>
        FallenGrass = 12,
        /// <summary>Pixel de muda folhosa solto da planta (ver <see cref="FallenGrass"/>).</summary>
        FallenLeaf = 13,
        /// <summary>
        /// Entulho: parede que os cupins arrancaram (ou canteiro que destruíram). Granular, sem marca: o jogador a marca para as formigas
        /// levarem embora, de preferência para a área de resíduos.
        /// </summary>
        Rubble = 14,
        /// <summary>Entulho depositado na área de resíduos. Fixo; some sozinho ao se decompor.</summary>
        Refuse = 15,
        /// <summary>
        /// Resto de cupim morto: na boca da formiga e depositado na área de resíduos. Fixo; ao se decompor vira folhas soltas
        /// (<see cref="FallenLeaf"/>) dentro da área.
        /// </summary>
        TermiteRemains = 16,
        /// <summary>
        /// Cogumelo luminoso: bloco de uma célula que as formigas encaixam no túnel (pincel Cogumelo luminoso) com um pixel de
        /// terra como substrato. Fixo, fonte de luz do mapa (<c>AntFarmView</c>); cavado, volta a ser terra solta na mão.
        /// Arte provisória: um cubo da cor de <see cref="CellMaterials.BaseColor"/>.
        /// </summary>
        GlowMushroom = 17,
    }

    /// <summary>Regras e cores por material. Tabela fixa enquanto o protótipo não pede dados configuráveis.</summary>
    public static class CellMaterials
    {
        public static bool IsSolid(CellMaterial material) => material != CellMaterial.Empty;

        /// <summary>Materiais que caem e escorregam em diagonal (formam pilhas).</summary>
        public static bool IsGranular(CellMaterial material) =>
            material == CellMaterial.Sand || material == CellMaterial.Spoil || material == CellMaterial.Mud ||
            material == CellMaterial.Rubble || IsPlantDebris(material);

        /// <summary>Terra fixa que a água amolece (vira <see cref="CellMaterial.Mud"/>) dentro do formigueiro.</summary>
        public static bool IsErodible(CellMaterial material) =>
            material == CellMaterial.Dirt || material == CellMaterial.Grass ||
            material == CellMaterial.PackedSpoil || material == CellMaterial.PackedSand;

        /// <summary>No que um material granular vira quando a chuva forte o compacta (os demais não mudam).</summary>
        public static CellMaterial ToPacked(CellMaterial material)
        {
            switch (material)
            {
                case CellMaterial.Sand: return CellMaterial.PackedSand;
                case CellMaterial.Spoil:
                case CellMaterial.Mud:
                case CellMaterial.GlowMushroom: return CellMaterial.PackedSpoil;
                default: return material;
            }
        }

        /// <summary>
        /// Planta da superfície: estática, não escavável pelo pincel de cavar; as formigas a colhem (pincel Colher) de
        /// cima para baixo e levam cada pixel para a sala de nutrientes.
        /// </summary>
        public static bool IsPlant(CellMaterial material) =>
            material == CellMaterial.PlantGrass || material == CellMaterial.PlantLeaf;

        /// <summary>
        /// Pixel de planta solto pela formiga (cai como grão): só então pode ser colhido e levado à sala de nutrientes.
        /// Mantém a marca de colheita do pixel que o originou.
        /// </summary>
        public static bool IsPlantDebris(CellMaterial material) =>
            material == CellMaterial.FallenGrass || material == CellMaterial.FallenLeaf;

        /// <summary>Planta em pé ou já solta: o que o pincel Colher marca.</summary>
        public static bool IsAnyPlant(CellMaterial material) => IsPlant(material) || IsPlantDebris(material);

        /// <summary>Em que a planta em pé vira ao ser solta (e de volta, ao ser colhida do chão).</summary>
        public static CellMaterial ToDebris(CellMaterial plant) =>
            plant == CellMaterial.PlantGrass ? CellMaterial.FallenGrass : CellMaterial.FallenLeaf;

        public static CellMaterial FromDebris(CellMaterial debris) =>
            debris == CellMaterial.FallenGrass ? CellMaterial.PlantGrass : CellMaterial.PlantLeaf;

        /// <summary>
        /// Aceita marca de trabalho (<see cref="PixelGrid.SetMarked"/>): escavável, planta, pixel solto de planta ou rocha.
        /// A marca na rocha pode existir antes de E2; a frente só avança quando há escavadora com E2 (design 25).
        /// </summary>
        public static bool IsMarkable(CellMaterial material) => IsDiggable(material) || IsAnyPlant(material) || material == CellMaterial.Rock;

        public static bool IsDiggable(CellMaterial material) =>
            material == CellMaterial.Dirt || material == CellMaterial.Grass ||
            material == CellMaterial.Sand || material == CellMaterial.Spoil ||
            material == CellMaterial.PackedSpoil || material == CellMaterial.PackedSand ||
            material == CellMaterial.Mud || material == CellMaterial.Rubble || material == CellMaterial.GlowMushroom;

        /// <summary>O que a formiga leva na mão ao cavar a célula: o cogumelo luminoso volta a ser o substrato (terra solta).</summary>
        public static CellMaterial ToCarried(CellMaterial dug) =>
            dug == CellMaterial.GlowMushroom ? CellMaterial.Spoil : dug;

        /// <summary>Segundos que uma formiga leva para soltar (ou colher) um pixel deste material.</summary>
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
                case CellMaterial.Mud: return 0.1f;
                case CellMaterial.Rubble: return 0.1f;
                case CellMaterial.PlantGrass: return 0.15f;
                case CellMaterial.PlantLeaf: return 0.2f;
                case CellMaterial.FallenGrass: return 0.15f;
                case CellMaterial.FallenLeaf: return 0.2f;
                case CellMaterial.GlowMushroom: return 0.3f;
                default: return float.PositiveInfinity;
            }
        }

        /// <summary>
        /// Vai para a área de resíduos quando há uma: entulho dos cupins e restos de cupim (<see cref="CellMaterial.TermiteRemains"/>
        /// na mão; <see cref="CellMaterial.Refuse"/> só aparece na mão em saves antigos).
        /// </summary>
        public static bool IsWaste(CellMaterial material) =>
            material == CellMaterial.Rubble || material == CellMaterial.Refuse || material == CellMaterial.TermiteRemains;

        /// <summary>Resíduo já depositado na área, que se decompõe com o tempo.</summary>
        public static bool IsDepositedWaste(CellMaterial material) =>
            material == CellMaterial.Refuse || material == CellMaterial.TermiteRemains;

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
                case CellMaterial.Mud: return new Color32(84, 58, 40, 255);
                case CellMaterial.PlantGrass: return new Color32(110, 168, 64, 255);
                case CellMaterial.PlantLeaf: return new Color32(62, 132, 70, 255);
                case CellMaterial.FallenGrass: return new Color32(110, 168, 64, 255);
                case CellMaterial.FallenLeaf: return new Color32(62, 132, 70, 255);
                case CellMaterial.Rubble: return new Color32(132, 96, 66, 255);
                case CellMaterial.Refuse: return new Color32(92, 80, 70, 255);
                case CellMaterial.TermiteRemains: return new Color32(112, 66, 58, 255);
                case CellMaterial.GlowMushroom: return new Color32(240, 214, 130, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }

        /// <summary>Cor final com a variação de tom da célula (0..255, 128 = neutro).</summary>
        public static Color32 ShadedColor(CellMaterial material, byte shade) => ShadedColor(material, shade, 1f);

        /// <summary>Como <see cref="ShadedColor(CellMaterial, byte)"/>, com a variação de tom reduzida por <paramref name="strength"/> (0 = cor base).</summary>
        public static Color32 ShadedColor(CellMaterial material, byte shade, float strength)
        {
            Color32 c = BaseColor(material);
            float f = 1f + strength * (shade * (0.36f / 255f) - 0.18f);
            return new Color32(Scale(c.r, f), Scale(c.g, f), Scale(c.b, f), 255);
        }

        static byte Scale(byte value, float factor)
        {
            int v = (int)(value * factor);
            return (byte)(v > 255 ? 255 : v);
        }
    }
}
