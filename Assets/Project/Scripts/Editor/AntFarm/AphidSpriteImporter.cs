using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>O pulgão usa 72x48 a 36 PPU: dois terços do canvas das formigas.</summary>
    public sealed class AphidSpriteImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Project/Art/Aphids/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".png"))
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
            settings.spritePixelsPerUnit = AntSpriteImporter.AdultPixelsPerCell;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = System.IO.Path.GetFileName(assetPath).StartsWith("Aphid_Top_")
                ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.25f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
        }
    }
}
