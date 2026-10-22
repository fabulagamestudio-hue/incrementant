using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using UnityEditor;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    /// <summary>
    /// Keeps <see cref="BuildAssistantSettings.RuntimeChangelog"/>, the release notes shipped with the build.
    /// The current session notes are recorded before the build starts (the build must carry its own notes) and
    /// rolled back if the build fails.
    /// </summary>
    public static class BuildAssistantChangelogService
    {
        private const string TimestampFormat = "yyyy-MM-dd HHmm";

        // Labels produced by BuildAssistantVersioningService.BuildVersionLabel: "v1.2.3[_suffix]" or "yyyyMMdd_HHmm[_suffix]".
        private static readonly Regex NumericLabelRegex = new(@"^v(\d+)\.(\d+)\.(\d+)(?:_(.+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DateTimeLabelRegex = new(@"^\d{8}_\d{4}(?:_(.+))?$", RegexOptions.Compiled);

        #region Public API

        /// <summary>Records the session notes for the build about to start. Returns the previous list for <see cref="Restore"/>.</summary>
        public static List<BuildAssistantChangelogEntry> PrepareForBuild(BuildAssistantSettings settings, BuildAssistantSessionState session, DateTime timestamp)
        {
            if (settings == null || session == null)
            {
                return null;
            }

            if (settings.RuntimeChangelog.Count == 0)
            {
                ImportHistory(settings);
            }

            var previous = new List<BuildAssistantChangelogEntry>(settings.RuntimeChangelog);
            settings.RecordChangelogEntry(CreateEntry(settings, session, timestamp));
            EditorUtility.SetDirty(settings);
            return previous;
        }

        public static void Restore(BuildAssistantSettings settings, List<BuildAssistantChangelogEntry> previous)
        {
            if (settings == null || previous == null)
            {
                return;
            }

            settings.ReplaceChangelog(previous);
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// Adds every successful history build whose version label is not in the changelog yet. Existing entries
        /// are kept, since they may have been edited by hand in the settings asset.
        /// </summary>
        public static int ImportHistory(BuildAssistantSettings settings)
        {
            if (settings == null || settings.RecentBuildHistory == null)
            {
                return 0;
            }

            var existingLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BuildAssistantChangelogEntry entry in settings.RuntimeChangelog)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.VersionLabel))
                {
                    existingLabels.Add(EntryKey(entry.BuildProfileId, entry.VersionLabel));
                }
            }

            int imported = 0;
            // Oldest first: if the history repeats a label, the newer build replaces the older one.
            for (int i = settings.RecentBuildHistory.Count - 1; i >= 0; i--)
            {
                BuildAssistantHistoryEntry history = settings.RecentBuildHistory[i];
                if (history == null || !history.WasSuccessful || string.IsNullOrWhiteSpace(history.VersionLabel) ||
                    existingLabels.Contains(EntryKey(history.BuildProfileId, history.VersionLabel)))
                {
                    continue;
                }

                settings.RecordChangelogEntry(CreateEntry(history));
                imported++;
            }

            if (imported > 0)
            {
                EditorUtility.SetDirty(settings);
            }

            return imported;
        }

        /// <summary>Menu entry: imports the history into the project settings asset and saves it.</summary>
        public static void ImportHistoryIntoLoadedSettings()
        {
            BuildAssistantSettings settings = BuildAssistantSettingsService.GetOrCreateSettings();
            if (settings == null)
            {
                Debug.LogError("Build Assistant: settings asset not found; nothing to import.");
                return;
            }

            int imported = ImportHistory(settings);
            BuildAssistantSettingsService.SaveSettings(settings);
            Debug.Log($"Build Assistant: {imported} version(s) imported into the runtime changelog ({settings.RuntimeChangelog.Count} total).", settings);
        }

        /// <summary>
        /// Translations of the session for the languages configured in the settings. Languages removed from the
        /// settings and translations without notes are left out of the build.
        /// </summary>
        public static List<BuildAssistantChangelogNotes> GetConfiguredTranslations(BuildAssistantSettings settings, BuildAssistantSessionState session)
        {
            var translations = new List<BuildAssistantChangelogNotes>();
            if (settings == null || session == null || session.ChangelogTranslations == null)
            {
                return translations;
            }

            foreach (string languageCode in settings.ChangelogTranslationLanguages)
            {
                BuildAssistantChangelogNotes notes = FindExact(session.ChangelogTranslations, languageCode);
                if (notes != null && notes.HasNotes)
                {
                    BuildAssistantChangelogNotes copy = notes.Clone();
                    copy.LanguageCode = languageCode.Trim();
                    copy.Added = Clean(copy.Added);
                    copy.Removed = Clean(copy.Removed);
                    copy.Modified = Clean(copy.Modified);
                    copy.KnownBugs = Clean(copy.KnownBugs);
                    translations.Add(copy);
                }
            }

            return translations;
        }

        /// <summary>Notes of the session in <paramref name="languageCode"/>, matched by the exact code; null when missing.</summary>
        public static BuildAssistantChangelogNotes FindExact(IEnumerable<BuildAssistantChangelogNotes> translations, string languageCode)
        {
            if (translations == null || string.IsNullOrWhiteSpace(languageCode))
            {
                return null;
            }

            foreach (BuildAssistantChangelogNotes notes in translations)
            {
                if (notes != null && string.Equals(notes.LanguageCode?.Trim(), languageCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return notes;
                }
            }

            return null;
        }

        #endregion

        #region Internal Logic

        private static BuildAssistantChangelogEntry CreateEntry(BuildAssistantSettings settings, BuildAssistantSessionState session, DateTime timestamp)
        {
            return new BuildAssistantChangelogEntry
            {
                VersionLabel = BuildAssistantVersioningService.BuildVersionLabel(session),
                Timestamp = timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                MajorVersion = Math.Max(0, session.MajorVersion),
                MinorVersion = Math.Max(0, session.MinorVersion),
                PatchVersion = Math.Max(0, session.PatchVersion),
                Suffix = Clean(session.ManualVersionSuffix),
                Profile = session.Profile.ToString(),
                BuildProfileId = Clean(session.BuildProfileId),
                BuildProfileName = Clean(session.BuildProfileName),
                Added = Clean(session.Added),
                Removed = Clean(session.Removed),
                Modified = Clean(session.Modified),
                KnownBugs = Clean(session.KnownBugs),
                LanguageCode = settings.ChangelogLanguage,
                Translations = GetConfiguredTranslations(settings, session)
            };
        }

        /// <summary>History does not store the version parts; they are recovered from the label when possible.</summary>
        private static BuildAssistantChangelogEntry CreateEntry(BuildAssistantHistoryEntry history)
        {
            string label = history.VersionLabel.Trim();
            var entry = new BuildAssistantChangelogEntry
            {
                VersionLabel = label,
                Timestamp = history.Timestamp,
                Suffix = string.Empty,
                Profile = Clean(history.Profile),
                BuildProfileId = Clean(history.BuildProfileId),
                BuildProfileName = Clean(history.BuildProfileName),
                Added = Clean(history.Added),
                Removed = Clean(history.Removed),
                Modified = Clean(history.Modified),
                KnownBugs = Clean(history.KnownBugs),
                LanguageCode = Clean(history.LanguageCode),
                Translations = BuildAssistantChangelogNotes.CloneAll(history.Translations)
            };

            Match numeric = NumericLabelRegex.Match(label);
            if (numeric.Success)
            {
                entry.MajorVersion = int.Parse(numeric.Groups[1].Value, CultureInfo.InvariantCulture);
                entry.MinorVersion = int.Parse(numeric.Groups[2].Value, CultureInfo.InvariantCulture);
                entry.PatchVersion = int.Parse(numeric.Groups[3].Value, CultureInfo.InvariantCulture);
                entry.Suffix = numeric.Groups[4].Value;
                return entry;
            }

            Match dateTime = DateTimeLabelRegex.Match(label);
            if (dateTime.Success)
            {
                entry.Suffix = dateTime.Groups[1].Value;
            }

            return entry;
        }

        /// <summary>A version is identified by its label within a Build Profile (entries without profile share one key).</summary>
        private static string EntryKey(string buildProfileId, string versionLabel) => Clean(buildProfileId) + "|" + Clean(versionLabel);

        private static string Clean(string text) => string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();

        #endregion
    }
}
