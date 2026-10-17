using System.Linq;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.EditorTools.AntFarm;
using Fabula.Incrementant.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fabula.Incrementant.EditorTools.Setup
{
    /// <summary>
    /// Cria/atualiza a cena do protótipo do formigário. Idempotente: reaproveita a cena, a câmera
    /// e os objetos existentes pelo nome, só criando o que faltar.
    /// </summary>
    public static class AntFarmSceneSetup
    {
        public const string ScenePath = "Assets/Project/Scenes/AntFarm.unity";
        const string CameraName = "Main Camera";
        const string RootName = "AntFarm";
        const string ViewName = "WorldView";
        const string NestName = "Nest";
        const string DumpStartName = "DumpStart";
        const string DumpEndName = "DumpEnd";
        const string AntsName = "Ants";
        const string SceneryName = "Scenery";
        const int DefaultSeed = 1234;

        [MenuItem("Incrementant/Setup/Criar cena AntFarm")]
        public static void CreateOrUpdate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = GetOrAdd<Camera>(FindOrCreate(scene, CameraName));
            camera.gameObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.06f, 0.05f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;

            GameObject root = FindOrCreate(scene, RootName);
            AntFarmGame game = GetOrAdd<AntFarmGame>(root);

            Transform viewTransform = root.transform.Find(ViewName);
            if (viewTransform == null)
            {
                viewTransform = new GameObject(ViewName).transform;
                viewTransform.SetParent(root.transform, false);
            }
            GetOrAdd<SpriteRenderer>(viewTransform.gameObject);
            AntFarmView view = GetOrAdd<AntFarmView>(viewTransform.gameObject);

            bool antsCreated = false;
            AntSwarmView antsView = GetOrAdd<AntSwarmView>(FindOrCreateChild(root.transform, AntsName, ref antsCreated).gameObject);
            AssignAntFrames(antsView);

            bool sceneryCreated = false;
            SurfaceSceneryView scenery = GetOrAdd<SurfaceSceneryView>(FindOrCreateChild(root.transform, SceneryName, ref sceneryCreated).gameObject);
            AssignSceneryArt(scenery);

            bool markersCreated = false;
            Transform nest = FindOrCreateChild(root.transform, NestName, ref markersCreated);
            Transform dumpStart = FindOrCreateChild(root.transform, DumpStartName, ref markersCreated);
            Transform dumpEnd = FindOrCreateChild(root.transform, DumpEndName, ref markersCreated);

            var serialized = new SerializedObject(game);
            serialized.FindProperty("_camera").objectReferenceValue = camera;
            serialized.FindProperty("_view").objectReferenceValue = view;
            serialized.FindProperty("_antsView").objectReferenceValue = antsView;
            serialized.FindProperty("_scenery").objectReferenceValue = scenery;
            serialized.FindProperty("_nestMarker").objectReferenceValue = nest;
            serialized.FindProperty("_dumpStartMarker").objectReferenceValue = dumpStart;
            serialized.FindProperty("_dumpEndMarker").objectReferenceValue = dumpEnd;

            // O mapa existente nunca é sobrescrito aqui; só é gerado se ainda não houver um.
            var map = serialized.FindProperty("_map").objectReferenceValue as Texture2D;
            if (map == null)
                map = AssetDatabase.LoadAssetAtPath<Texture2D>(AntFarmMapTools.DefaultMapPath);
            if (map == null)
            {
                TerrainLayout layout = AntFarmMapTools.GenerateMapAsset(AntFarmMapTools.DefaultMapPath, new TerrainSettings(), DefaultSeed, out int dumpY);
                map = AssetDatabase.LoadAssetAtPath<Texture2D>(AntFarmMapTools.DefaultMapPath);
                serialized.FindProperty("_map").objectReferenceValue = map;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AntFarmMapTools.PlaceMarkers(game, layout, dumpY);
            }
            else
            {
                serialized.FindProperty("_map").objectReferenceValue = map;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (markersCreated)
                    Debug.LogWarning("[AntFarmSceneSetup] Marcadores de ninho/despejo criados na origem: posicione-os sobre o mapa.", root);
            }

            // O modo do mapa (Procedural por padrão) é escolha do usuário e não é alterado aqui; o PNG fica
            // atribuído para o modo Image. O preview segue o modo: PNG na cena (Image) ou nenhum (Procedural).
            AntFarmGameEditor.SyncPreview(game);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureInBuildSettings();
            Debug.Log($"[AntFarmSceneSetup] Cena pronta em {ScenePath}.");
        }

        static void EnsureInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
                return;
            // Primeira da lista: é a cena jogável do protótipo; a SampleScene do template fica atrás.
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // TryGetComponent evita o "null falso" que GetComponent devolve no Editor (quebra o operador ??).
        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T component) ? component : go.AddComponent<T>();

        /// <summary>Gera só os quadros que faltam (não sobrescreve retoques de arte) e os atribui.</summary>
        static void AssignAntFrames(AntSwarmView antsView)
        {
            var serialized = new SerializedObject(antsView);
            SetSprites(serialized.FindProperty("_walkFrames"), AntSpriteGenerator.Generate(overwrite: false));
            SetSprites(serialized.FindProperty("_topFrames"), AntSpriteGenerator.GenerateTop(overwrite: false));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Exporta só os elementos do cenário que ainda não têm PNG (não sobrescreve retoques) e liga o catálogo.</summary>
        static void AssignSceneryArt(SurfaceSceneryView scenery)
        {
            var serialized = new SerializedObject(scenery);
            serialized.FindProperty("_art").objectReferenceValue = SceneryArtExporter.EnsureArt();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetSprites(SerializedProperty list, Sprite[] sprites)
        {
            list.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        static Transform FindOrCreateChild(Transform parent, string name, ref bool created)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            created = true;
            return child;
        }

        static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);

        static GameObject FindOrCreate(Scene scene, string name)
        {
            GameObject existing = FindRoot(scene, name);
            if (existing != null)
                return existing;
            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }
    }
}
