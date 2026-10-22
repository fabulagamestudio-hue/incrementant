using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Imagens do glossário (<see cref="Folder"/>, uma por entrada, com o id da entrada como nome): Sprite de pixel art, sem
    /// filtro, sem compressão e sem mipmap, para a janela mostrar o PNG como foi gravado.
    /// </summary>
    public sealed class GlossaryImageImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Project/Art/UI/Glossary/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = false;
        }
    }
}
