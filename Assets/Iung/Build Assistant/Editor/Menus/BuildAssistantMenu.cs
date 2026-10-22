using Iung.BuildAssistant.Services;
using Iung.BuildAssistant.Editor.Windows;
using UnityEditor;

namespace Iung.BuildAssistant.Editor.Menus
{
    public static class BuildAssistantMenu
    {
        #region Public API

        [MenuItem("Iung/Build Assistant/Open")]
        public static void OpenWindow()
        {
            BuildAssistantWindow.OpenWindow();
        }

        [MenuItem("Iung/Build Assistant/Select Settings")]
        public static void SelectSettings()
        {
            BuildAssistantSettingsService.SelectSettingsAsset();
        }

        [MenuItem("Iung/Build Assistant/Import History Into Runtime Changelog")]
        public static void ImportHistoryIntoChangelog()
        {
            BuildAssistantChangelogService.ImportHistoryIntoLoadedSettings();
        }

        #endregion
    }
}
