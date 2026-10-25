using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// Sprites em <see cref="AntsFolder"/>: pixel art sem filtro nem compressão, adultos com
    /// <see cref="WorldPixelsPerCell"/> pixels por célula (os da arte antiga, <see cref="AdultPixelsPerCell"/>) e cria com
    /// <see cref="PixelsPerCell"/>.
    /// De lado: pivô no meio da largura e meia célula acima das patas (ver AntSwarmView).
    /// Vista de cima (<see cref="TopViewPrefix"/>): pivô no centro.
    /// </summary>
    public sealed class AntSpriteImporter : AssetPostprocessor
    {
        public const string AntsFolder = "Assets/Project/Art/Ants/";
        /// <summary>Densidade base, preservada para cenário e cria.</summary>
        public const float PixelsPerCell = 6f;
        public const int AdultArtScale = 6;
        public const float AdultPixelsPerCell = PixelsPerCell * AdultArtScale;
        public const string TopViewPrefix = "Ant_Top_";
        /// <summary>
        /// Densidade do mundo: px de arte por célula, a mesma do terreno (<see cref="Fabula.Incrementant.AntFarm.CellTileArt.TexelsPerCell"/>).
        /// Experimento 8x8 de 2026-10-22: 8 (antes 16, documento 40); os adultos (24x16 = 3x2 células) usam esta.
        /// </summary>
        public const float WorldPixelsPerCell = Fabula.Incrementant.AntFarm.CellTileArt.TexelsPerCell;
        /// <summary>Quadros próprios da rainha (64x32 no experimento 8x8, com 8 px de folga de cada lado para a coroa e a barriga; 96x64 antes).</summary>
        public const string QueenFolder = AntsFolder + "Rainha/";
        /// <summary>
        /// A rainha é desenhada com o dobro do tamanho (<c>AntSwarmView</c> → <b>Queen Scale</b> 2): a 2 × WorldPixelsPerCell px por unidade,
        /// cada pixel dos quadros continua do tamanho do texel do terreno. Mudar o Queen Scale pede mudar este valor junto.
        /// </summary>
        public const float QueenPixelsPerUnit = WorldPixelsPerCell * 2f;

        /// <summary>
        /// Quadro adulto ainda na arte antiga (108x72 ou 144x144, a 36 px por célula): rainha e cupim alado até serem
        /// redesenhados na etapa 8. O tamanho vem do PNG, então trocar o arquivo pelo redesenhado basta para passar a 16.
        /// </summary>
        static bool IsLegacyAdult(TextureImporter importer)
        {
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            return (width == 108 && height == 72) || (width == 144 && height == 144);
        }

        /// <summary>Itens que a formiga leva na boca (1 célula, etapa 9; 8x8 no experimento 8x8): pivô no centro.</summary>
        public const string ItemPrefix = "Item_";

        /// <summary>Ovo, larva ou pupa ainda na arte antiga (18x12 a 6 px por célula); os redesenhados (48x32) usam 16.</summary>
        static bool IsLegacyBrood(TextureImporter importer)
        {
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            return width == 18 && height == 12;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AntsFolder))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            string fileName = System.IO.Path.GetFileName(assetPath);
            bool adult = fileName.StartsWith("Ant_Walk_") || fileName.StartsWith(TopViewPrefix)
                || fileName.StartsWith("Ant_Fly_") || fileName.StartsWith("Ant_Land_");
            settings.spritePixelsPerUnit = !adult ? (IsLegacyBrood(importer) ? PixelsPerCell : WorldPixelsPerCell)
                : IsLegacyAdult(importer) ? AdultPixelsPerCell
                : assetPath.StartsWith(QueenFolder) ? QueenPixelsPerUnit
                : WorldPixelsPerCell;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // Quadros laterais compartilham a âncora; o inimigo usa canvas 144x144 com espaço para as asas.
            bool centered = fileName.StartsWith(TopViewPrefix) || fileName.StartsWith(ItemPrefix);
            settings.spritePivot = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 3f / 12f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
        }
    }
}
