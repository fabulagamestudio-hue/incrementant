using System.Linq;
using Fabula.Incrementant.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Fabula.Incrementant.EditorTools.Setup
{
    /// <summary>
    /// Pós-processamento do jogo (etapa 7 do documento 40): perfil com bloom leve só nos tons claros, vinheta e ajuste de
    /// cor quente, num Volume global com <see cref="PostProcessSettingsBinder"/>, e a câmera com pós-processamento ligado.
    /// Idempotente: o perfil só recebe os valores iniciais quando um efeito ainda não existe nele (retoques ficam).
    /// </summary>
    public static class PostProcessSetup
    {
        public const string ProfilePath = "Assets/Project/Settings/PostProcessProfile.asset";
        const string VolumeName = "PostProcess";
        const string CameraName = "Main Camera";

        [MenuItem("Incrementant/Setup/Configurar pós-processamento")]
        static void RunOnActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject cameraObject = scene.GetRootGameObjects().FirstOrDefault(go => go.name == CameraName);
            if (cameraObject == null || !cameraObject.TryGetComponent(out Camera camera))
            {
                Debug.LogError($"[PostProcessSetup] A cena '{scene.name}' não tem '{CameraName}' na raiz; rode o setup da cena antes.");
                return;
            }
            Build(scene, camera);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PostProcessSetup] Pós-processamento configurado em {scene.path}.");
        }

        /// <summary>Liga o pós-processamento na câmera e garante o Volume global com o perfil. Não salva a cena.</summary>
        public static void Build(Scene scene, Camera camera)
        {
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            EditorUtility.SetDirty(camera.GetUniversalAdditionalCameraData());

            GameObject go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == VolumeName);
            if (go == null)
            {
                go = new GameObject(VolumeName);
                SceneManager.MoveGameObjectToScene(go, scene);
            }
            Volume volume = go.TryGetComponent(out Volume existing) ? existing : go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = EnsureProfile();
            if (!go.TryGetComponent(out PostProcessSettingsBinder _))
                go.AddComponent<PostProcessSettingsBinder>();
        }

        static VolumeProfile EnsureProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // Limiar acima do branco (linear): só o que emite em HDR brilha, hoje o cogumelo luminoso (emissão no shader do
            // terreno). Decisão do desenvolvedor em 2026-10-21: por ora, bloom só nos cogumelos, não na superfície clara.
            if (Add(profile, out Bloom bloom))
            {
                bloom.threshold.Override(1.1f);
                // Intensidade 1 mal aparecia no cogumelo; com 6 iterações e espalhamento de 0,55 ou mais, o halo virava uma
                // névoa grande em volta da galeria. 3 iterações deixam o halo perto do cubo (2026-10-21).
                // Subiu para 3 a pedido do desenvolvedor (mais brilho nos cogumelos); 3 iterações seguram o halo perto do cubo.
                bloom.intensity.Override(3f);
                bloom.scatter.Override(0.4f);
                bloom.maxIterations.Override(3);
                bloom.downscale.Override(BloomDownscaleMode.Half);
                bloom.highQualityFiltering.Override(false);
            }
            if (Add(profile, out Vignette vignette))
            {
                vignette.intensity.Override(0.25f);
                vignette.smoothness.Override(0.45f);
                vignette.color.Override(Color.black);
            }
            // Temperatura em vez de filtro de cor: esquenta sem escurecer a paleta.
            if (Add(profile, out WhiteBalance whiteBalance))
                whiteBalance.temperature.Override(10f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return profile;
        }

        /// <summary>Acrescenta o efeito como sub-asset do perfil; devolve true só quando ele foi criado agora.</summary>
        static bool Add<T>(VolumeProfile profile, out T component) where T : VolumeComponent
        {
            if (profile.TryGet(out component))
                return false;
            component = profile.Add<T>();
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; // como o editor do perfil faz
            AssetDatabase.AddObjectToAsset(component, profile);
            return true;
        }
    }
}
