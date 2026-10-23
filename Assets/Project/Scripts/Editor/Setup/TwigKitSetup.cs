using Fabula.Incrementant.AntFarm;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fabula.Incrementant.EditorTools.Setup
{
    /// <summary>
    /// Kit do graveto (2026-10-23, experimento 8x8): cria ou atualiza <see cref="KitPath"/> com as peças de
    /// <see cref="PiecesFolder"/> e liga o kit no <see cref="SurfaceSceneryView"/> da cena ativa, que passa a montar os
    /// gravetos do cenário por ele. Idempotente: refaz as referências das peças e não troca um kit que já esteja ligado.
    /// </summary>
    public static class TwigKitSetup
    {
        public const string PiecesFolder = "Assets/Project/Art/SceneryKits/Twig/";
        public const string KitPath = PiecesFolder + "TwigKit.asset";

        [MenuItem("Incrementant/Setup/Criar kit do graveto")]
        static void RunOnActiveScene()
        {
            TwigKit kit = EnsureKit();
            if (kit == null)
                return;
            Scene scene = SceneManager.GetActiveScene();
            SurfaceSceneryView view = Object.FindFirstObjectByType<SurfaceSceneryView>();
            if (view == null || view.gameObject.scene != scene)
            {
                Debug.LogError($"[TwigKitSetup] A cena '{scene.name}' não tem SurfaceSceneryView; abra a cena AntFarm e rode de novo.");
                return;
            }
            var serialized = new SerializedObject(view);
            SerializedProperty field = serialized.FindProperty("_twigKit");
            if (field.objectReferenceValue == null)
            {
                field.objectReferenceValue = kit;
                serialized.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[TwigKitSetup] Kit do graveto em {KitPath}, ligado em {scene.path}.");
        }

        /// <summary>Cria o asset se faltar e liga as peças pelos nomes dos PNGs.</summary>
        public static TwigKit EnsureKit()
        {
            Texture2D Load(string file)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PiecesFolder + file);
                if (texture == null)
                    Debug.LogError($"[TwigKitSetup] Falta {PiecesFolder}{file}; gere as peças com Source~/twig_kit.py.");
                return texture;
            }

            Texture2D bark = Load("bark.png"), cut = Load("cut_end.png"), broken = Load("broken_end.png"), tip = Load("branch_tip.png");
            Texture2D[] knots = { Load("knot_0.png"), Load("knot_1.png"), Load("knot_2.png") };
            if (bark == null || cut == null || broken == null || tip == null || System.Array.Exists(knots, k => k == null))
                return null;

            var kit = AssetDatabase.LoadAssetAtPath<TwigKit>(KitPath);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<TwigKit>();
                AssetDatabase.CreateAsset(kit, KitPath);
            }
            var serialized = new SerializedObject(kit);
            serialized.FindProperty("_bark").objectReferenceValue = bark;
            serialized.FindProperty("_cutEnd").objectReferenceValue = cut;
            serialized.FindProperty("_brokenEnd").objectReferenceValue = broken;
            serialized.FindProperty("_branchTip").objectReferenceValue = tip;
            SerializedProperty array = serialized.FindProperty("_knots");
            array.arraySize = knots.Length;
            for (int i = 0; i < knots.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = knots[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return kit;
        }
    }
}
