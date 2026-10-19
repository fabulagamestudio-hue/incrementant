using System.Linq;
using Fabula.Incrementant.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Monta a cena do splash da Fábula (<c>Scenes/Splash.unity</c>) e a coloca em primeiro no build, antes do menu principal.
    /// Idempotente: a cena é refeita do zero a cada execução. A imagem é importada como sprite de pixel art (Point, sem
    /// mipmap nem compressão).
    /// </summary>
    public static class SplashSceneSetup
    {
        public const string ScenePath = "Assets/Project/Scenes/Splash.unity";
        public const string ImagePath = "Assets/Project/Art/UI/splash-fabula-fundo.png";
        public const string LogoPath = "Assets/Project/Art/UI/splash-fabula-logo.png";

        // Cor do fundo atrás da imagem (o marrom quase preto da arte), para telas que não sejam 16:9.
        static readonly Color Background = new Color(0.047f, 0.027f, 0.016f);

        [MenuItem("Incrementant/Setup/Criar cena Splash")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            ImportImage(ImagePath, false);
            ImportImage(LogoPath, true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ImagePath);
            if (sprite == null)
            {
                Debug.LogError($"[SplashSceneSetup] Imagem ausente: {ImagePath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = true;
            camera.cullingMask = 0; // só a UI desenha

            var canvasObject = new GameObject("Splash", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SplashScreen));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var imageObject = new GameObject("Imagem", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            imageObject.layer = LayerMask.NameToLayer("UI");
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            // Preenche a tela mantendo a proporção da arte (corta só o excedente em telas que não sejam 16:9).
            var fitter = imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

            var so = new SerializedObject(canvasObject.GetComponent<SplashScreen>());
            so.FindProperty("_fade").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();
            ConfigureLogo(canvasObject);
            ConfigureDissolve(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureFirstInBuild();
            Debug.Log($"[SplashSceneSetup] Splash pronta em {ScenePath} (primeira do build).");
        }

        // Atualização localizada: preserva câmera, tempos e demais objetos da cena existente.
        public static void UpdateLayers()
        {
            ImportImage(ImagePath, false);
            ImportImage(LogoPath, true);
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var splash = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SplashScreen>(true)).Single();
                var so = new SerializedObject(splash);
                var background = (Image)so.FindProperty("_fade").objectReferenceValue;
                background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ImagePath);
                background.GetComponent<AspectRatioFitter>().aspectRatio = background.sprite.rect.width / background.sprite.rect.height;
                ConfigureLogo(splash.gameObject);
                ConfigureDissolve(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        // A dissolução do logotipo (VFX Graph) só entra se já foi gerada por "Splash - dissolução da logo".
        static void ConfigureDissolve(Scene scene)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(SplashDissolveSetup.VfxPath) != null)
                SplashDissolveSetup.ConfigureScene(scene);
        }

        static void ConfigureLogo(GameObject canvasObject)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            if (sprite == null)
                throw new System.InvalidOperationException($"Logotipo ausente: {LogoPath}");
            var existing = canvasObject.transform.Find("Logotipo");
            var logoObject = existing != null ? existing.gameObject :
                new GameObject("Logotipo", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            logoObject.layer = LayerMask.NameToLayer("UI");
            logoObject.transform.SetParent(canvasObject.transform, false);
            logoObject.transform.SetAsLastSibling();
            var rect = (RectTransform)logoObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var image = logoObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var fitter = logoObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            var so = new SerializedObject(canvasObject.GetComponent<SplashScreen>());
            so.FindProperty("_logo").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ImportImage(string path, bool transparent)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                return;
            bool dirty = importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point ||
                         importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                         importer.alphaIsTransparency != transparent || importer.spriteImportMode != SpriteImportMode.Single;
            if (!dirty)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = transparent;
            importer.SaveAndReimport();
        }

        static void EnsureFirstInBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
