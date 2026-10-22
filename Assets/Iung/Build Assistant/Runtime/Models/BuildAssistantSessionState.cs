using System;
using System.Collections.Generic;
using UnityEngine;

namespace Iung.BuildAssistant.Models
{
    [Serializable]
    public sealed class BuildAssistantSessionState
    {
        #region Fields

        /// <summary>Unity Build Profile (or platform) this session belongs to; see <c>BuildAssistantBuildProfileService</c>.</summary>
        public string BuildProfileId = string.Empty;
        public string BuildProfileName = string.Empty;
        public BuildAssistantPlatform Platform = BuildAssistantPlatform.Windows;
        public BuildAssistantProfile Profile = BuildAssistantProfile.Development;
        public string OutputFolder = string.Empty;
        public bool AutoVersionIfTargetExists = true;
        public bool OverwriteExistingBuild;

        public string CompanyName = string.Empty;
        public string ProductName = string.Empty;
        public string ExecutableName = string.Empty;
        public Texture2D AppIcon;
        public Texture2D SplashTexture;

        public bool RunInBackground = true;
        public FullScreenMode FullScreenMode = FullScreenMode.Windowed;
        public int DefaultScreenWidth = 1280;
        public int DefaultScreenHeight = 720;
        public bool ResizableWindow = true;

        public BuildAssistantVersioningMode VersioningMode = BuildAssistantVersioningMode.Numeric;
        public int MajorVersion = 1;
        public int MinorVersion;
        public int PatchVersion;
        public string ManualVersionSuffix = string.Empty;
        public string Added = string.Empty;
        public string Removed = string.Empty;
        public string Modified = string.Empty;
        public string KnownBugs = string.Empty;
        /// <summary>Notes in the extra languages listed in the settings; the fields above are the main language.</summary>
        public List<BuildAssistantChangelogNotes> ChangelogTranslations = new();
        public BuildAssistantScriptChangeReport ScriptChangeReport = new();
        public List<BuildAssistantFolderNameTokenRule> FolderNameComposition = BuildAssistantFolderNameCompositionUtility.CreateDefault();

        public string PreviewBuildName = string.Empty;
        public string PreviewBuildPath = string.Empty;
        public string ProgressTitle = "Idle";
        public string ProgressMessage = string.Empty;
        public string TechnicalLog = string.Empty;
        public bool BuildStarted;
        public bool LastBuildSucceeded;

        #endregion
    }
}
