using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Configura a fonte da UI, a Press Start 2P (<c>Art/UI/Fontes/PressStart2P</c>): fonte dinâmica (qualquer caractere,
    /// inclusive os acentos do português) com rasterização "Hinted Raster", que alinha os glifos à grade de pixels.
    /// A fonte é desenhada numa grade de 8 px, então o HUD só usa tamanhos múltiplos de 8. Idempotente.
    /// </summary>
    public static class UiFontImporter
    {
        public const string Folder = "Assets/Project/Art/UI/Fontes/PressStart2P";
        public const string FontPath = Folder + "/PressStart2P-Regular.ttf";

        [MenuItem("Incrementant/UI/Aplicar importação das fontes")]
        public static void Apply()
        {
            if (!(AssetImporter.GetAtPath(FontPath) is TrueTypeFontImporter importer))
            {
                Debug.LogError($"[UiFontImporter] Fonte ausente: {FontPath}");
                return;
            }
            bool dirty = importer.fontTextureCase != FontTextureCase.Dynamic ||
                         importer.fontRenderingMode != FontRenderingMode.HintedRaster ||
                         !importer.includeFontData;
            if (dirty)
            {
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.fontRenderingMode = FontRenderingMode.HintedRaster;
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }
            Debug.Log($"[UiFontImporter] {(dirty ? 1 : 0)} fonte(s) da UI reconfiguradas.");
        }

        public static Font Load() => AssetDatabase.LoadAssetAtPath<Font>(FontPath);
    }
}
