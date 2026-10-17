using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.Setup
{
    /// <summary>
    /// Imagens em <see cref="MapsFolder"/> são mapas do formigário: importadas sem filtro, sem compressão e sem mipmap
    /// (o valor exato de cada pixel define o material), legíveis em runtime e como Sprite de 1 unidade por pixel
    /// com pivô no canto inferior esquerdo, para servir também de preview na cena.
    /// </summary>
    public sealed class AntFarmMapImporter : AssetPostprocessor
    {
        public const string MapsFolder = "Assets/Project/Art/Maps/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(MapsFolder))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.alphaIsTransparency = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = 1f;
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
        }
    }
}
