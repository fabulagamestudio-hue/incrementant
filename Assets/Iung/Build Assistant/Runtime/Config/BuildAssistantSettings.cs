using System;
using System.Collections.Generic;
using Iung.BuildAssistant.Models;
using UnityEngine;

namespace Iung.BuildAssistant.Config
{
    [CreateAssetMenu(fileName = "BuildAssistantSettings", menuName = "Iung/Build Assistant/Settings")]
    public sealed class BuildAssistantSettings : ScriptableObject
    {
        #region Fields

        [SerializeField]
        [Tooltip("Default company name used to initialize new build sessions.")]
        private string defaultCompanyName = string.Empty;

        [SerializeField]
        [Tooltip("Default product name used to initialize new build sessions.")]
        private string defaultProductName = string.Empty;

        [SerializeField]
        [Tooltip("Default output folder used when the current session does not define one yet.")]
        private string defaultOutputFolder = "Builds";

        [SerializeField]
        [Tooltip("Maximum number of build history entries retained by the plugin.")]
        private int maxHistoryEntries = 20;

        [SerializeField]
        [Tooltip("Default folder name composition used by Build Assistant (order and visibility).")]
        private List<BuildAssistantFolderNameTokenRule> defaultFolderNameComposition = BuildAssistantFolderNameCompositionUtility.CreateDefault();

        [SerializeField]
        [Tooltip("Recent build history entries recorded by the assistant.")]
        private List<BuildAssistantHistoryEntry> recentBuildHistory = new();

        [SerializeField]
        [Tooltip("Last Build Assistant session values used to restore the editor window state.")]
        private BuildAssistantSessionState lastSessionState = new();

        [SerializeField]
        [Tooltip("Build Assistant configuration of each Unity Build Profile (output folder, identity, version, notes, folder name). The profile active in Unity picks which one the window edits.")]
        private List<BuildAssistantProfileSession> profileSessions = new();

        [SerializeField]
        [Tooltip("Build Profile id of the build being prepared; the runtime changelog shows only the versions of this profile.")]
        private string runtimeBuildProfileId = string.Empty;

        [SerializeField]
        [Tooltip("Version label prepared for runtime UI labels before the build starts.")]
        private string runtimeVersionLabel = string.Empty;

        [SerializeField]
        [Tooltip("Timestamp prepared for runtime UI labels before the build starts.")]
        private string runtimeVersionTimestamp = string.Empty;

        [SerializeField]
        [Tooltip("Release notes shipped with the build, newest first. One entry per version; not trimmed by the history limit.")]
        private List<BuildAssistantChangelogEntry> runtimeChangelog = new();

        [SerializeField]
        [Tooltip("Language of the main changelog notes (Added, Modified, Removed, Known Bugs). Used at runtime when the player's language has no translation.")]
        private string changelogLanguage = "en";

        [SerializeField]
        [Tooltip("Extra changelog languages (e.g. pt-BR). Each one gets its own notes tab in the Versioning step.")]
        private List<string> changelogTranslationLanguages = new();

        #endregion

        #region Properties

        public string DefaultCompanyName => defaultCompanyName;
        public string DefaultProductName => defaultProductName;
        public string DefaultOutputFolder => defaultOutputFolder;
        public int MaxHistoryEntries => maxHistoryEntries;
        public List<BuildAssistantFolderNameTokenRule> DefaultFolderNameComposition => defaultFolderNameComposition;
        public List<BuildAssistantHistoryEntry> RecentBuildHistory => recentBuildHistory;
        public BuildAssistantSessionState LastSessionState => lastSessionState;
        public IReadOnlyList<BuildAssistantProfileSession> ProfileSessions => profileSessions;
        public string RuntimeBuildProfileId => runtimeBuildProfileId ?? string.Empty;
        public string RuntimeVersionLabel => runtimeVersionLabel;
        public string RuntimeVersionTimestamp => runtimeVersionTimestamp;
        public IReadOnlyList<BuildAssistantChangelogEntry> RuntimeChangelog => runtimeChangelog;
        public string ChangelogLanguage => string.IsNullOrWhiteSpace(changelogLanguage) ? string.Empty : changelogLanguage.Trim();
        public IReadOnlyList<string> ChangelogTranslationLanguages => changelogTranslationLanguages;

        public void UpdateLastSessionState(BuildAssistantSessionState sessionState)
        {
            if (sessionState == null)
            {
                return;
            }

            if (lastSessionState == null)
            {
                lastSessionState = new BuildAssistantSessionState();
            }

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(sessionState), lastSessionState);
            EnsureConsistency();
        }

        /// <summary>Saved session of <paramref name="profileId"/>; null when that profile was never configured.</summary>
        public BuildAssistantSessionState FindProfileSession(string profileId)
        {
            BuildAssistantProfileSession entry = FindProfileEntry(profileId);
            return entry != null ? entry.Session : null;
        }

        public void UpdateProfileSession(string profileId, string profileName, BuildAssistantSessionState sessionState)
        {
            if (sessionState == null)
            {
                return;
            }

            profileSessions ??= new List<BuildAssistantProfileSession>();
            BuildAssistantProfileSession entry = FindProfileEntry(profileId);
            if (entry == null)
            {
                entry = new BuildAssistantProfileSession { ProfileId = NormalizeProfileId(profileId), Session = new BuildAssistantSessionState() };
                profileSessions.Add(entry);
            }

            entry.ProfileName = profileName ?? string.Empty;
            entry.Session ??= new BuildAssistantSessionState();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(sessionState), entry.Session);
            entry.Session.FolderNameComposition = BuildAssistantFolderNameCompositionUtility.Normalize(entry.Session.FolderNameComposition);
            entry.Session.ChangelogTranslations ??= new List<BuildAssistantChangelogNotes>();
        }

        /// <summary>
        /// Versions listed by the runtime changelog UI: the ones built with the same profile as the shipped build,
        /// plus entries recorded before profiles were tracked (no id), which stay visible everywhere.
        /// </summary>
        public List<BuildAssistantChangelogEntry> GetRuntimeChangelogForBuild()
        {
            var result = new List<BuildAssistantChangelogEntry>();
            if (runtimeChangelog == null)
            {
                return result;
            }

            string buildProfileId = RuntimeBuildProfileId;
            foreach (BuildAssistantChangelogEntry entry in runtimeChangelog)
            {
                if (entry == null)
                {
                    continue;
                }

                string entryProfileId = NormalizeProfileId(entry.BuildProfileId);
                if (entryProfileId.Length == 0 || buildProfileId.Length == 0 ||
                    string.Equals(entryProfileId, buildProfileId, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        public void UpdateDefaultFolderNameComposition(IList<BuildAssistantFolderNameTokenRule> composition)
        {
            defaultFolderNameComposition = BuildAssistantFolderNameCompositionUtility.Normalize(composition);
        }

        public void UpdateRuntimeVersionState(BuildAssistantSessionState sessionState, string versionLabel, DateTime timestamp)
        {
            UpdateLastSessionState(sessionState);
            runtimeBuildProfileId = NormalizeProfileId(sessionState != null ? sessionState.BuildProfileId : null);
            runtimeVersionLabel = string.IsNullOrWhiteSpace(versionLabel) ? string.Empty : versionLabel.Trim();
            runtimeVersionTimestamp = timestamp.ToString("yyyy-MM-dd HHmm");
        }

        /// <summary>
        /// Adds or replaces the notes of <paramref name="entry"/>'s version (matched by version label and Build
        /// Profile) and keeps the list newest first.
        /// </summary>
        public void RecordChangelogEntry(BuildAssistantChangelogEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.VersionLabel))
            {
                return;
            }

            runtimeChangelog ??= new List<BuildAssistantChangelogEntry>();
            string versionLabel = entry.VersionLabel.Trim();
            string profileId = NormalizeProfileId(entry.BuildProfileId);
            runtimeChangelog.RemoveAll(existing =>
                existing == null ||
                (string.Equals(existing.VersionLabel?.Trim(), versionLabel, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(NormalizeProfileId(existing.BuildProfileId), profileId, StringComparison.OrdinalIgnoreCase)));
            runtimeChangelog.Add(entry);
            // Timestamp is "yyyy-MM-dd HHmm", so ordinal order is chronological order.
            runtimeChangelog.Sort((left, right) => string.CompareOrdinal(right.Timestamp, left.Timestamp));
        }

        public void ReplaceChangelog(IEnumerable<BuildAssistantChangelogEntry> entries)
        {
            runtimeChangelog = entries != null ? new List<BuildAssistantChangelogEntry>(entries) : new List<BuildAssistantChangelogEntry>();
        }

        public void EnsureConsistency()
        {
            defaultFolderNameComposition = BuildAssistantFolderNameCompositionUtility.Normalize(defaultFolderNameComposition);

            if (lastSessionState == null)
            {
                lastSessionState = new BuildAssistantSessionState();
            }

            lastSessionState.FolderNameComposition =
                BuildAssistantFolderNameCompositionUtility.Normalize(lastSessionState.FolderNameComposition);

            changelogTranslationLanguages ??= new List<string>();
            lastSessionState.ChangelogTranslations ??= new List<BuildAssistantChangelogNotes>();
            profileSessions ??= new List<BuildAssistantProfileSession>();
        }

        private BuildAssistantProfileSession FindProfileEntry(string profileId)
        {
            if (profileSessions == null)
            {
                return null;
            }

            string id = NormalizeProfileId(profileId);
            for (int i = 0; i < profileSessions.Count; i++)
            {
                BuildAssistantProfileSession entry = profileSessions[i];
                if (entry != null && string.Equals(NormalizeProfileId(entry.ProfileId), id, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private static string NormalizeProfileId(string profileId)
        {
            return string.IsNullOrWhiteSpace(profileId) ? string.Empty : profileId.Trim();
        }

        private void OnEnable()
        {
            EnsureConsistency();
        }

        private void OnValidate()
        {
            EnsureConsistency();
        }

        #endregion
    }
}
