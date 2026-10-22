using System.IO;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Editor.Windows;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    [InitializeOnLoad]
    public static class BuildAssistantSettingsService
    {
        #region Initialization

        static BuildAssistantSettingsService()
        {
            EditorApplication.delayCall += EnsureSettingsAssetExists;
        }

        #endregion

        #region Public API

        public static BuildAssistantSettings GetOrCreateSettings()
        {
            string settingsAssetPath = ResolveSettingsAssetPath();
            if (!string.IsNullOrWhiteSpace(settingsAssetPath))
            {
                BuildAssistantSettings settings = AssetDatabase.LoadAssetAtPath<BuildAssistantSettings>(settingsAssetPath);
                if (settings != null)
                {
                    settings.EnsureConsistency();
                    return settings;
                }
            }

            EnsureSettingsAssetExists();
            settingsAssetPath = ResolveSettingsAssetPath();
            if (string.IsNullOrWhiteSpace(settingsAssetPath))
            {
                return null;
            }

            BuildAssistantSettings createdSettings = AssetDatabase.LoadAssetAtPath<BuildAssistantSettings>(settingsAssetPath);
            if (createdSettings != null)
            {
                createdSettings.EnsureConsistency();
            }

            return createdSettings;
        }

        public static void SelectSettingsAsset()
        {
            BuildAssistantSettings settings = GetOrCreateSettings();
            if (settings == null)
            {
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        public static void EnsureSettingsAssetExists()
        {
            string settingsAssetPath = ResolveSettingsAssetPath();
            if (string.IsNullOrWhiteSpace(settingsAssetPath))
            {
                return;
            }

            BuildAssistantSettings existingSettings = AssetDatabase.LoadAssetAtPath<BuildAssistantSettings>(settingsAssetPath);
            if (existingSettings != null)
            {
                existingSettings.EnsureConsistency();
                return;
            }

            if (BuildAssistantPathUtility.IsPackagesPath(settingsAssetPath))
            {
                Debug.LogError($"[BuildAssistant] Settings asset was not found at '{settingsAssetPath}'. The package must include this asset in its Config folder.");
                return;
            }

            EnsureFolderPathExists(Path.GetDirectoryName(settingsAssetPath));

            BuildAssistantSettings settings = ScriptableObject.CreateInstance<BuildAssistantSettings>();
            settings.EnsureConsistency();
            try
            {
                AssetDatabase.CreateAsset(settings, settingsAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[BuildAssistant] Failed to create settings asset at '{settingsAssetPath}'. {exception.Message}");
                Object.DestroyImmediate(settings);
            }
        }

        public static void SaveSettings(BuildAssistantSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Internal Logic

        private static string ResolveSettingsAssetPath()
        {
            return BuildAssistantPathUtility.NormalizeAssetPath(BuildAssistantPaths.SettingsAssetPath);
        }

        private static void EnsureFolderPathExists(string folderPath)
        {
            string normalizedFolderPath = BuildAssistantPathUtility.NormalizeAssetPath(folderPath);
            if (string.IsNullOrWhiteSpace(normalizedFolderPath))
            {
                return;
            }

            if (!BuildAssistantPathUtility.IsAssetsPath(normalizedFolderPath))
            {
                Debug.LogError($"[BuildAssistant] Settings folder path is invalid: '{normalizedFolderPath}'.");
                return;
            }

            string[] segments = normalizedFolderPath.Split('/');
            string currentPath = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]) || segments[i] == "." || segments[i] == "..")
                {
                    continue;
                }

                string nextPath = currentPath + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[i]);
                }

                currentPath = nextPath;
            }
        }

        #endregion
    }

    public sealed class BuildAssistantBuildInterceptor : IPreprocessBuildWithReport
    {
        private const string DialogTitle = "Build Assistant";
        private const string DialogMessage =
            "A build was started outside Build Assistant.\n\n" +
            "Do you want to open Build Assistant and cancel this build?";
        private const string OpenAssistantButton = "Open Build Assistant";
        private const string ContinueDefaultButton = "Continue Default Build";

        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (BuildAssistantBuildContext.IsAssistantBuild)
            {
                return;
            }

            if (Application.isBatchMode)
            {
                return;
            }

            bool openAssistant = EditorUtility.DisplayDialog(
                DialogTitle,
                DialogMessage,
                OpenAssistantButton,
                ContinueDefaultButton);

            if (!openAssistant)
            {
                return;
            }

            BuildAssistantWindow.OpenWindow();
            throw new BuildFailedException("Build canceled by Build Assistant interceptor.");
        }
    }
}
