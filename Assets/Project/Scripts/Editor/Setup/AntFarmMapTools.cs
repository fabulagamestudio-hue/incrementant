using System.IO;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.EditorTools.AntFarm;
using Fabula.Incrementant.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.Setup
{
    /// <summary>Gera o mapa do formigário como imagem (PNG) e posiciona os marcadores da cena.</summary>
    public static class AntFarmMapTools
    {
        public const string DefaultMapPath = AntFarmMapImporter.MapsFolder + "AntFarm_Map01.png";

        [MenuItem("Incrementant/Mapa/Sortear e fixar mapa na cena (sobrescreve o PNG)")]
        static void RegenerateFromMenu()
        {
            AntFarmGame game = Object.FindFirstObjectByType<AntFarmGame>();
            if (game == null)
            {
                Debug.LogError("[AntFarmMapTools] Abra a cena AntFarm antes de gerar o mapa.");
                return;
            }

            var serialized = new SerializedObject(game);
            var map = serialized.FindProperty("_map").objectReferenceValue as Texture2D;
            string path = map != null ? AssetDatabase.GetAssetPath(map) : DefaultMapPath;

            var settings = serialized.FindProperty("_terrainSettings").boxedValue as TerrainSettings ?? new TerrainSettings();
            TerrainLayout layout = GenerateMapAsset(path, settings, Random.Range(int.MinValue, int.MaxValue), out int dumpY);
            serialized.FindProperty("_map").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            // Fixar um mapa sorteado só faz sentido no modo Image.
            serialized.FindProperty("_mapSource").enumValueIndex = (int)MapSource.Image;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PlaceMarkers(game, layout, dumpY);
            AntFarmGameEditor.SyncPreview(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Debug.Log($"[AntFarmMapTools] Mapa gerado em {path}. Salve a cena para manter os marcadores reposicionados.");
        }

        /// <summary>Gera um mapa procedural, grava em PNG no caminho dado e importa. <paramref name="dumpY"/> = linha logo acima do solo no despejo.</summary>
        public static TerrainLayout GenerateMapAsset(string assetPath, TerrainSettings settings, int seed, out int dumpY)
        {
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, seed);
            dumpY = grid.GetSurfaceY(layout.DumpMinX) + 1;

            var texture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(TerrainMapCodec.Encode(grid));
                texture.Apply(false);
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return layout;
        }

        /// <summary>Coloca ninho e despejo nas posições do layout (centro da célula, em unidades de mundo).</summary>
        public static void PlaceMarkers(AntFarmGame game, TerrainLayout layout, int dumpY)
        {
            var serialized = new SerializedObject(game);
            var view = serialized.FindProperty("_view").objectReferenceValue as AntFarmView;
            Vector3 origin = view != null ? view.transform.position : Vector3.zero;

            SetCell(serialized, "_nestMarker", origin, layout.Nest.x, layout.Nest.y);
            SetCell(serialized, "_dumpStartMarker", origin, layout.DumpMinX, dumpY);
            SetCell(serialized, "_dumpEndMarker", origin, layout.DumpMaxX, dumpY);
        }

        static void SetCell(SerializedObject serialized, string property, Vector3 origin, int x, int y)
        {
            if (serialized.FindProperty(property).objectReferenceValue is Transform marker)
                marker.position = origin + new Vector3(x + 0.5f, y + 0.5f, 0f);
        }
    }
}
