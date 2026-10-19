using System.Linq;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.EditorTools.AntFarm;
using Fabula.Incrementant.EditorTools.UI;
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
        const string BroodName = "Brood";
        const string CultivationName = "Cultivation";
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

            BuildWorld(scene);
            AntFarmHudSetup.Build(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureInBuildSettings();
            Debug.Log($"[AntFarmSceneSetup] Cena pronta em {ScenePath}.");
        }

        /// <summary>
        /// Câmera, o formigário (<see cref="AntFarmGame"/> e suas views), o cenário e o mapa na cena dada, sem HUD e sem
        /// salvar. Compartilhado com a cena do menu principal (<c>MainMenuSetup</c>). Idempotente.
        /// </summary>
        public static AntFarmGame BuildWorld(Scene scene)
        {
            Camera camera = GetOrAdd<Camera>(FindOrCreate(scene, CameraName));
            camera.gameObject.tag = "MainCamera";
            GetOrAdd<AudioListener>(camera.gameObject); // sem ele nada toca (SFX, chuva, música)
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
            AssignTileMaterial(view);

            bool antsCreated = false;
            AntSwarmView antsView = GetOrAdd<AntSwarmView>(FindOrCreateChild(root.transform, AntsName, ref antsCreated).gameObject);
            AssignAntFrames(antsView);

            bool broodCreated = false;
            BroodView broodView = GetOrAdd<BroodView>(FindOrCreateChild(root.transform, BroodName, ref broodCreated).gameObject);
            AssignBroodFrames(broodView);

            bool cultivationCreated = false;
            CultivationView cultivationView =
                GetOrAdd<CultivationView>(FindOrCreateChild(root.transform, CultivationName, ref cultivationCreated).gameObject);

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
            serialized.FindProperty("_broodView").objectReferenceValue = broodView;
            serialized.FindProperty("_cultivationView").objectReferenceValue = cultivationView;
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
            return game;
        }

        const string TileMaterialPath = "Assets/Project/Materials/TerrainTiles.mat";
        const string TileShaderName = "Incrementant/TerrainTiles";

        /// <summary>Material do shader de tiles 16x16 do terreno: cria se faltar e só atribui se o campo estiver vazio.</summary>
        static void AssignTileMaterial(AntFarmView view)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TileMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find(TileShaderName);
                if (shader == null)
                {
                    Debug.LogError($"[AntFarmSceneSetup] Shader {TileShaderName} não encontrado (Assets/Project/Materials/TerrainTiles.shader); o terreno fica com 1 texel por célula.", view);
                    return;
                }
                material = new Material(shader) { name = "TerrainTiles" };
                AssetDatabase.CreateAsset(material, TileMaterialPath);
            }
            var serialized = new SerializedObject(view);
            SerializedProperty property = serialized.FindProperty("_tileMaterial");
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = material;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
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
            AssignRoleArt(serialized.FindProperty("_roleArt"));
            AssignTermiteArt(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Pasta dos cupins (a arte "formiga inimiga alada" do REQ-027).</summary>
        const string TermiteFolder = "InimigaAlada";

        /// <summary>Quadros dos cupins: voo, pouso (asas caindo), caminhada de lado e vista de cima.</summary>
        static void AssignTermiteArt(SerializedObject antsView)
        {
            string folder = $"{AntSpriteImporter.AntsFolder}{TermiteFolder}/";
            SetSprites(antsView.FindProperty("_termiteFly"), LoadFrames(folder, "Ant_Fly_"));
            SetSprites(antsView.FindProperty("_termiteLand"), LoadFrames(folder, "Ant_Land_"));
            SetSprites(antsView.FindProperty("_termiteWalk"), LoadFrames(folder, "Ant_Walk_"));
            SetSprites(antsView.FindProperty("_termiteTop"), LoadFrames(folder, AntSpriteImporter.TopViewPrefix));
        }

        /// <summary>Papel → pasta de arte em Art/Ants (a rainha fica de fora: usa os quadros da raiz).</summary>
        static readonly (AntRole role, string folder)[] RoleArtFolders =
        {
            (AntRole.Worker, "Operaria"),
            (AntRole.Gardener, "Jardineira"),
            (AntRole.Carrier, "Coletora"),
            (AntRole.Cutter, "Cortadeira"),
            (AntRole.Digger, "Escavadora"),
            (AntRole.Nurse, "Cuidadora"),
            (AntRole.Soldier, "Soldado"),
        };

        /// <summary>Atualiza só a arte por papel do AntSwarmView na cena já pronta, sem refazer o resto (HUD, mapa, cenário).</summary>
        [MenuItem("Incrementant/Arte/Ligar arte das formigas por papel e dos cupins")]
        static void AssignRoleArtToScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AntSwarmView antsView = Object.FindFirstObjectByType<AntSwarmView>();
            if (antsView == null)
            {
                Debug.LogError("[AntFarmSceneSetup] AntSwarmView não encontrado: rode Incrementant/Setup/Criar cena AntFarm.");
                return;
            }
            var serialized = new SerializedObject(antsView);
            AssignRoleArt(serialized.FindProperty("_roleArt"));
            AssignTermiteArt(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AntFarmSceneSetup] Arte por papel e dos cupins ligada em AntSwarmView.");
        }

        /// <summary>Primeiro quadro de caminhada do papel (usado como ícone no HUD). Null se a pasta não tiver arte.</summary>
        public static Sprite RoleIcon(AntRole role)
        {
            foreach ((AntRole r, string folder) in RoleArtFolders)
                if (r == role)
                    return AssetDatabase.LoadAssetAtPath<Sprite>($"{AntSpriteImporter.AntsFolder}{folder}/Ant_Walk_0.png");
            return null;
        }

        static void AssignRoleArt(SerializedProperty list)
        {
            list.arraySize = RoleArtFolders.Length;
            for (int i = 0; i < RoleArtFolders.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Role").enumValueIndex = (int)RoleArtFolders[i].role; // valores 0..6 contíguos
                string folder = $"{AntSpriteImporter.AntsFolder}{RoleArtFolders[i].folder}/";
                SetSprites(entry.FindPropertyRelative("Walk"), LoadFrames(folder, "Ant_Walk_"));
                SetSprites(entry.FindPropertyRelative("Top"), LoadFrames(folder, AntSpriteImporter.TopViewPrefix));
            }
        }

        static Sprite[] LoadFrames(string folder, string prefix) => Enumerable.Range(0, AntSpriteGenerator.FrameCount)
            .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}{prefix}{i}.png"))
            .Where(sprite => sprite != null)
            .ToArray();

        /// <summary>Liga os quadros de ovo, larva e pupa (PNGs em Art/Ants, desenhados à mão: nunca são gerados nem sobrescritos).</summary>
        static void AssignBroodFrames(BroodView broodView)
        {
            var serialized = new SerializedObject(broodView);
            SetSprites(serialized.FindProperty("_eggFrames"), LoadIdleFrames("Egg"));
            SetSprites(serialized.FindProperty("_larvaFrames"), LoadIdleFrames("Larva"));
            SetSprites(serialized.FindProperty("_pupaFrames"), LoadIdleFrames("Pupa"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite[] LoadIdleFrames(string prefix) => Enumerable.Range(0, 2)
            .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{AntSpriteImporter.AntsFolder}{prefix}_Idle_{i}.png"))
            .Where(sprite => sprite != null)
            .ToArray();

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
