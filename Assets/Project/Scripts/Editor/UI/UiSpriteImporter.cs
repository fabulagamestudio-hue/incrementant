using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Importa os PNGs de <c>Assets/Project/Art/UI/Sprites</c> como sprites de UI (Point, sem mipmap, sem compressão) e
    /// grava as bordas 9-slice (esquerda, baixo, direita, cima, em pixels) dos que esticam. Idempotente: só reimporta o que
    /// mudou. A pasta <c>FormigueiroSkillTree</c> é outro conjunto de arte e não é tocada.
    /// </summary>
    public static class UiSpriteImporter
    {
        public const string SpritesFolder = "Assets/Project/Art/UI/Sprites";

        // Moldura de madeira dos painéis ~8 px; botões e barras têm cantos arredondados menores. Sprites que não
        // esticam (ícones, checkbox, toggle, badge, alça do slider) ficam sem borda.
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            ["panels/hud_card"] = new Vector4(9, 9, 9, 9),
            ["panels/panel_plain"] = new Vector4(9, 9, 9, 9),
            ["panels/tooltip"] = new Vector4(9, 9, 9, 9),
            ["panels/notification"] = new Vector4(9, 9, 9, 9),
            ["panels/panel_header"] = new Vector4(9, 9, 9, 16),
            ["panels/panel_moss"] = new Vector4(12, 12, 12, 12),
            ["tabs/tab_active"] = new Vector4(9, 9, 9, 9),
            ["tabs/tab_inactive"] = new Vector4(9, 9, 9, 9),
            ["buttons/button_normal"] = new Vector4(7, 7, 7, 7),
            ["buttons/button_pressed"] = new Vector4(7, 7, 7, 7),
            ["buttons/button_focus"] = new Vector4(7, 7, 7, 7),
            ["buttons/button_action"] = new Vector4(7, 7, 7, 7),
            ["buttons/button_danger"] = new Vector4(7, 7, 7, 7),
            ["buttons/button_disabled"] = new Vector4(7, 7, 7, 7),
            ["buttons/icon_button"] = new Vector4(9, 9, 9, 9),
            ["buttons/icon_button_focus"] = new Vector4(9, 9, 9, 9),
            ["buttons/icon_button_disabled"] = new Vector4(9, 9, 9, 9),
            ["progress/progress_track"] = new Vector4(6, 4, 6, 4),
            ["progress/progress_green"] = new Vector4(6, 4, 6, 4),
            ["progress/progress_amber"] = new Vector4(6, 4, 6, 4),
            ["sliders/slider_track"] = new Vector4(6, 4, 6, 4),
            ["sliders/slider_fill"] = new Vector4(6, 4, 6, 4),
            ["scrollbar/scrollbar_track"] = new Vector4(4, 6, 4, 6),
            ["scrollbar/scrollbar_thumb"] = new Vector4(4, 6, 4, 6),
        };

        [MenuItem("Incrementant/UI/Aplicar importação e 9-slice dos sprites")]
        public static void Apply()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesFolder });
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("/FormigueiroSkillTree/"))
                        continue;
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer && Configure(importer, path))
                    {
                        importer.SaveAndReimport();
                        changed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log($"[UiSpriteImporter] {changed} sprite(s) de UI reconfigurados.");
        }

        static bool Configure(TextureImporter importer, string path)
        {
            string key = path.Substring(SpritesFolder.Length + 1);
            key = key.Substring(0, key.Length - ".png".Length);
            Vector4 border = Borders.TryGetValue(key, out Vector4 b) ? b : Vector4.zero;

            bool dirty = false;
            dirty |= Set(importer.textureType != TextureImporterType.Sprite, () => importer.textureType = TextureImporterType.Sprite);
            dirty |= Set(importer.spriteImportMode != SpriteImportMode.Single, () => importer.spriteImportMode = SpriteImportMode.Single);
            dirty |= Set(importer.filterMode != FilterMode.Point, () => importer.filterMode = FilterMode.Point);
            dirty |= Set(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
            dirty |= Set(importer.textureCompression != TextureImporterCompression.Uncompressed,
                         () => importer.textureCompression = TextureImporterCompression.Uncompressed);
            dirty |= Set(!importer.alphaIsTransparency, () => importer.alphaIsTransparency = true);
            dirty |= Set(importer.spriteBorder != border, () => importer.spriteBorder = border);
            return dirty;
        }

        static bool Set(bool needed, System.Action apply)
        {
            if (needed)
                apply();
            return needed;
        }
    }
}
