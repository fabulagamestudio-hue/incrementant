using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// Sprites em <see cref="AntsFolder"/>: pixel art sem filtro nem compressão, <see cref="PixelsPerCell"/> pixels
    /// por célula do terreno. De lado: pivô no meio da largura e meia célula acima das patas (ver AntSwarmView).
    /// Vista de cima (<see cref="TopViewPrefix"/>): pivô no centro.
    /// </summary>
    public sealed class AntSpriteImporter : AssetPostprocessor
    {
        public const string AntsFolder = "Assets/Project/Art/Ants/";
        /// <summary>Densidade da arte das formigas: 6 pixels de arte por pixel do terreno.</summary>
        public const float PixelsPerCell = 6f;
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
            settings.spritePixelsPerUnit = PixelsPerCell;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // 18x12. De lado: 3 px (meia célula) acima da linha das patas. De cima: centro.
            bool topView = System.IO.Path.GetFileName(assetPath).StartsWith(TopViewPrefix);
            settings.spritePivot = topView ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 3f / 12f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
        }
    }
}
