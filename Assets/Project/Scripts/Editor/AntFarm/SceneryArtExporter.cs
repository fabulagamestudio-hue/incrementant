using System.IO;
using System.Linq;
using Fabula.Incrementant.AntFarm;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.AntFarm
{
    /// <summary>
    /// Exporta cada elemento do cenário da superfície como PNG isolado em <see cref="SceneryArtImporter.SceneryFolder"/>
    /// (uma pasta por elemento, <c>&lt;Elemento&gt;_NN.png</c>, pivô no ponto de apoio no chão) e mantém o catálogo
    /// <see cref="ArtSetPath"/> com os sprites de cada pasta, que o <see cref="SurfaceSceneryView"/> usa para montar o cenário.
    /// </summary>
    public static class SceneryArtExporter
    {
        public const string ArtSetPath = "Assets/Project/Data/SceneryArtSet.asset";

        internal static bool IsExporting { get; private set; }

        public static string FolderOf(SceneryElement element) => SceneryArtImporter.SceneryFolder + element switch
        {
            SceneryElement.Grass => "Grass",
            SceneryElement.Tuft => "Tufts",
            SceneryElement.Flower => "Flowers",
            SceneryElement.Clover => "Clovers",
            SceneryElement.Mushroom => "Mushrooms",
            SceneryElement.Twig => "Twigs",
            SceneryElement.DryLeaf => "DryLeaves",
            SceneryElement.Pebble => "Pebbles",
            SceneryElement.Grain => "Grains",
            _ => element.ToString(),
        };

        static SceneryElement[] Elements => (SceneryElement[])System.Enum.GetValues(typeof(SceneryElement));

        [MenuItem("Incrementant/Arte/Exportar elementos do cenário (sobrescreve)")]
        static void ExportFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Exportar elementos do cenário",
                    $"Regrava as variantes procedurais em {SceneryArtImporter.SceneryFolder}, sobrescrevendo os PNGs de mesmo nome " +
                    "(retoques feitos neles se perdem). Arquivos com outros nomes ficam.", "Exportar", "Cancelar"))
                return;
            Export(overwrite: true);
        }

        [MenuItem("Incrementant/Arte/Atualizar catálogo do cenário")]
        static void RefreshFromMenu() => RefreshArtSet();

        /// <summary>Garante arte e catálogo: exporta só os elementos cuja pasta ainda não tem PNG. Usado pelo setup da cena.</summary>
        public static SceneryArtSet EnsureArt() => Export(overwrite: false);

        /// <summary>
        /// Grava as variantes de <see cref="SceneryStampLibrary.CreateProcedural"/>. Sem <paramref name="overwrite"/>,
        /// pula elementos que já têm algum PNG na pasta (para não recriar variantes que o artista apagou).
        /// </summary>
        public static SceneryArtSet Export(bool overwrite)
        {
            SceneryStampLibrary library = null;
            var written = new System.Collections.Generic.List<(string path, SceneryStamp stamp)>();
            IsExporting = true;
            try
            {
                foreach (SceneryElement element in Elements)
                {
                    string folder = FolderOf(element);
                    if (!overwrite && Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.png").Any())
                        continue;
                    Directory.CreateDirectory(folder);
                    library ??= SceneryStampLibrary.CreateProcedural(SceneryStampLibrary.DefaultSeed);
                    SceneryStamp[] stamps = library.Get(element);
                    for (int i = 0; i < stamps.Length; i++)
                    {
                        string path = $"{folder}/{element}_{i:00}.png";
                        WritePng(path, stamps[i]);
                        written.Add((path, stamps[i]));
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach ((string path, SceneryStamp stamp) in written)
                    SetPivot(path, stamp);
            }
            finally
            {
                IsExporting = false;
            }

            if (written.Count > 0)
                Debug.Log($"Cenário: {written.Count} PNGs de elementos exportados em {SceneryArtImporter.SceneryFolder}.");
            return RefreshArtSet();
        }

        /// <summary>Cria/atualiza o catálogo com os sprites de cada pasta, em ordem de nome.</summary>
        public static SceneryArtSet RefreshArtSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<SceneryArtSet>(ArtSetPath);
            if (set == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ArtSetPath));
                set = ScriptableObject.CreateInstance<SceneryArtSet>();
                AssetDatabase.CreateAsset(set, ArtSetPath);
            }

            foreach (SceneryElement element in Elements)
            {
                string folder = FolderOf(element);
                Sprite[] sprites = AssetDatabase.IsValidFolder(folder)
                    ? AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                                   .Select(AssetDatabase.GUIDToAssetPath)
                                   .Distinct()
                                   .OrderBy(p => p, System.StringComparer.Ordinal)
                                   .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                                   .Where(s => s != null)
                                   .ToArray()
                    : System.Array.Empty<Sprite>();
                set.Set(element, sprites);
            }
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssetIfDirty(set);
            return set;
        }

        static void WritePng(string path, SceneryStamp stamp)
        {
            var texture = new Texture2D(stamp.Width, stamp.Height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(stamp.Pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        static void SetPivot(string path, SceneryStamp stamp)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(stamp.PivotX / (float)stamp.Width, stamp.PivotY / (float)stamp.Height);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
