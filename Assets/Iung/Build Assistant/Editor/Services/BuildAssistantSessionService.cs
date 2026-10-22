using System.IO;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using UnityEditor;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantSessionService
    {
        #region Public API

        public static BuildAssistantSessionState CreateDefaultSession(BuildAssistantSettings settings, bool restorePersistedSession = true)
        {
            BuildAssistantSessionState session = new BuildAssistantSessionState
            {
                CompanyName = string.IsNullOrWhiteSpace(settings != null ? settings.DefaultCompanyName : string.Empty)
                    ? PlayerSettings.companyName
                    : settings.DefaultCompanyName,
                ProductName = string.IsNullOrWhiteSpace(settings != null ? settings.DefaultProductName : string.Empty)
                    ? PlayerSettings.productName
                    : settings.DefaultProductName,
                ExecutableName = PlayerSettings.productName,
                RunInBackground = PlayerSettings.runInBackground,
                DefaultScreenWidth = PlayerSettings.defaultScreenWidth,
                DefaultScreenHeight = PlayerSettings.defaultScreenHeight,
                FullScreenMode = PlayerSettings.fullScreenMode,
                ResizableWindow = PlayerSettings.resizableWindow,
                OutputFolder = ResolveDefaultOutputFolder(settings),
                FolderNameComposition = BuildAssistantFolderNameCompositionUtility.Normalize(
                    settings != null ? settings.DefaultFolderNameComposition : null)
            };

            BuildAssistantBuildProfileService.ApplyActiveProfile(session);
            if (restorePersistedSession)
            {
                TryRestorePersistedSession(settings, session);
            }

            EnsureFolderNameComposition(settings, session);
            ResetTransientFields(session);
            return session;
        }

        public static void RefreshPreview(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return;
            }

            EnsureFolderNameComposition(null, session);
            session.PreviewBuildName = BuildAssistantVersioningService.BuildOutputFileName(session);
            session.PreviewBuildPath = BuildAssistantVersioningService.BuildOutputPath(session);
        }

        public static void PersistSession(BuildAssistantSettings settings, BuildAssistantSessionState session)
        {
            if (settings == null || session == null)
            {
                return;
            }

            BuildAssistantSessionState snapshot = CloneSession(session);
            if (snapshot == null)
            {
                return;
            }

            EnsureFolderNameComposition(settings, snapshot);
            ResetTransientFields(snapshot);
            settings.UpdateDefaultFolderNameComposition(snapshot.FolderNameComposition);
            settings.UpdateProfileSession(snapshot.BuildProfileId, snapshot.BuildProfileName, snapshot);
            settings.UpdateLastSessionState(snapshot);
            BuildAssistantSettingsService.SaveSettings(settings);
        }

        #endregion

        #region Internal Logic

        /// <summary>
        /// Restores the session saved for the active Build Profile. A profile never configured starts from the last
        /// session (so output folder, identity and version are a starting point), without the release notes when
        /// those belong to another profile.
        /// </summary>
        private static void TryRestorePersistedSession(BuildAssistantSettings settings, BuildAssistantSessionState targetSession)
        {
            if (settings == null || targetSession == null)
            {
                return;
            }

            BuildAssistantSessionState source = settings.FindProfileSession(targetSession.BuildProfileId);
            bool inheritedFromOtherProfile = false;
            if (source == null)
            {
                source = settings.LastSessionState;
                inheritedFromOtherProfile = source != null &&
                                            !string.IsNullOrEmpty(source.BuildProfileId) &&
                                            source.BuildProfileId != targetSession.BuildProfileId;
            }

            if (source == null)
            {
                return;
            }

            string serializedSession = JsonUtility.ToJson(source);
            if (string.IsNullOrWhiteSpace(serializedSession) || serializedSession == "{}")
            {
                return;
            }

            JsonUtility.FromJsonOverwrite(serializedSession, targetSession);
            BuildAssistantBuildProfileService.ApplyActiveProfile(targetSession);
            if (inheritedFromOtherProfile)
            {
                targetSession.Added = string.Empty;
                targetSession.Removed = string.Empty;
                targetSession.Modified = string.Empty;
                targetSession.KnownBugs = string.Empty;
                targetSession.ChangelogTranslations = new System.Collections.Generic.List<BuildAssistantChangelogNotes>();
            }

            EnsureFolderNameComposition(settings, targetSession);
        }

        private static BuildAssistantSessionState CloneSession(BuildAssistantSessionState sourceSession)
        {
            if (sourceSession == null)
            {
                return null;
            }

            string serializedSession = JsonUtility.ToJson(sourceSession);
            if (string.IsNullOrWhiteSpace(serializedSession))
            {
                return null;
            }

            return JsonUtility.FromJson<BuildAssistantSessionState>(serializedSession);
        }

        private static void ResetTransientFields(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return;
            }

            session.PreviewBuildName = string.Empty;
            session.PreviewBuildPath = string.Empty;
            session.ProgressTitle = "Idle";
            session.ProgressMessage = string.Empty;
            session.TechnicalLog = string.Empty;
            session.BuildStarted = false;
            session.LastBuildSucceeded = false;
            session.ScriptChangeReport = BuildAssistantScriptChangeService.RefreshReport(session.BuildProfileId);
        }

        private static void EnsureFolderNameComposition(BuildAssistantSettings settings, BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return;
            }

            System.Collections.Generic.IList<BuildAssistantFolderNameTokenRule> source = session.FolderNameComposition;
            if (source == null || source.Count == 0)
            {
                source = settings != null ? settings.DefaultFolderNameComposition : null;
            }

            session.FolderNameComposition = BuildAssistantFolderNameCompositionUtility.Normalize(source);
        }

        private static string ResolveDefaultOutputFolder(BuildAssistantSettings settings)
        {
            string configuredFolder = settings != null ? settings.DefaultOutputFolder : string.Empty;
            if (string.IsNullOrWhiteSpace(configuredFolder))
            {
                configuredFolder = "Builds";
            }

            if (Path.IsPathRooted(configuredFolder))
            {
                return BuildAssistantPathUtility.NormalizeSystemPath(configuredFolder);
            }

            string projectFolder = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return BuildAssistantPathUtility.NormalizeSystemPath(Path.Combine(projectFolder, configuredFolder));
        }

        #endregion
    }
}
