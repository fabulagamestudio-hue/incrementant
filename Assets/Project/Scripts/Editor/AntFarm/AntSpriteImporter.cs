using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// Sprites em <see cref="AntsFolder"/>: pixel art sem filtro nem compressão, adultos com
    /// <see cref="AdultPixelsPerCell"/> pixels por célula e cria com <see cref="PixelsPerCell"/>.
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
            settings.spritePixelsPerUnit = adult ? AdultPixelsPerCell : PixelsPerCell;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // Quadros laterais compartilham a âncora; o inimigo usa canvas 144x144 com espaço para as asas.
            bool topView = fileName.StartsWith(TopViewPrefix);
            settings.spritePivot = topView ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 3f / 12f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
        }
    }
}
