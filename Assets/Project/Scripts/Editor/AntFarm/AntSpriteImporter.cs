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
        /// <summary>Densidade do mundo (documento 40): 16 px de arte por célula. Os adultos redesenhados (48x32 = 3x2 células) usam esta.</summary>
        public const float WorldPixelsPerCell = 16f;
        /// <summary>Quadros próprios da rainha (96x64).</summary>
        public const string QueenFolder = AntsFolder + "Rainha/";
        /// <summary>
        /// A rainha é desenhada com o dobro do tamanho (<c>AntSwarmView</c> → <b>Queen Scale</b> 2): a 32 px por unidade,
        /// cada pixel dos quadros de 96x64 continua sendo 1/16 de célula. Mudar o Queen Scale pede mudar este valor junto.
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

        /// <summary>Itens que a formiga leva na boca (16x16 = 1 célula, etapa 9): pivô no centro.</summary>
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
