using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// Peças dos kits de cenário em <see cref="Folder"/> (piloto do graveto, 2026-10-23): textura comum, Point, sem
    /// compressão nem mipmap, Read/Write ligado (o <c>TwigKitBuilder</c> lê os texels) e Repeat (a casca repete).
    /// </summary>
    public sealed class SceneryKitImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Project/Art/SceneryKits/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
        }
    }
}
