using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Importa o kit <c>Art/UI/Sprites/FormigueiroSkillTree/Textures</c> como sprite de pixel art (Point, sem mipmap,
    /// sem compressão, malha completa) e grava as bordas 9-slice das peças que esticam. É outro conjunto que o
    /// <c>UiSpriteImporter</c>, que não toca nesta pasta. Idempotente.
    /// </summary>
    public static class SkillTreeSpriteImporter
    {
        public const string Folder = "Assets/Project/Art/UI/Sprites/FormigueiroSkillTree/Textures";

        // Bordas (L, B, R, T) em pixels do sprite, medidas nas peças de 38x36 (abas), 64x64 (painel) e 54x26 (botões).
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            ["Tabs/tab_active"] = new Vector4(12, 12, 12, 12),
            ["Tabs/tab_inactive"] = new Vector4(12, 12, 12, 12),
            ["Panels/skill_details_panel"] = new Vector4(8, 8, 8, 8),
            ["Buttons/button_unlock"] = new Vector4(8, 8, 8, 8),
            ["Buttons/button_back"] = new Vector4(8, 8, 8, 8),
        };

        [MenuItem("Incrementant/UI/Aplicar importação dos sprites da árvore")]
        public static void Apply()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
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
            Debug.Log($"[SkillTreeSpriteImporter] {changed} sprite(s) da árvore reconfigurados.");
        }

        static bool Configure(TextureImporter importer, string path)
        {
            string key = path.Substring(Folder.Length + 1);
            key = key.Substring(0, key.Length - ".png".Length);
            Vector4 border = Borders.TryGetValue(key, out Vector4 b) ? b : Vector4.zero;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool dirty = importer.textureType != TextureImporterType.Sprite ||
                         settings.spriteMode != (int)SpriteImportMode.Single ||
                         settings.filterMode != FilterMode.Point || settings.mipmapEnabled ||
                         !settings.alphaIsTransparency || settings.spriteBorder != border ||
                         settings.spriteMeshType != SpriteMeshType.FullRect ||
                         importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!dirty)
                return false;
            importer.textureType = TextureImporterType.Sprite;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.spriteBorder = border;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            return true;
        }
    }
}
