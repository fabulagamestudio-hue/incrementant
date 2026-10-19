using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Importa os PNGs de <c>Assets/Project/Art/UI/Cursors</c> como cursores do sistema (tipo Cursor, Point, sem mipmap,
    /// sem compressão), que é o que <c>Cursor.SetCursor</c> exige. Roda sozinho na importação e vale também para o HUD setup.
    /// </summary>
    public class UiCursorImporter : AssetPostprocessor
    {
        public const string CursorsFolder = "Assets/Project/Art/UI/Cursors";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CursorsFolder + "/"))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Cursor;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
