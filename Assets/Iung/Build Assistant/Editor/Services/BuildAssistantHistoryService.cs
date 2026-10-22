using System.IO;
using System.Text;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using UnityEditor;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantHistoryService
    {
        private const string LatestBuildLogFileName = "BuildAssistant_LastBuildLog.txt";
        private const string CumulativeBuildLogFileName = "BuildAssistant_AllBuildLogs.txt";

        #region Public API

        public static void Record(BuildAssistantSettings settings, BuildAssistantSessionState session, string finalOutputPath, bool wasSuccessful, long buildSizeBytes = -1)
        {
            if (settings == null || session == null)
            {
                return;
            }

            BuildAssistantHistoryEntry entry = new BuildAssistantHistoryEntry
            {
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HHmm"),
                Platform = BuildAssistantActivePlatformService.GetActivePlatformLabel(),
                Profile = string.Empty,
                BuildProfileId = session.BuildProfileId,
                BuildProfileName = session.BuildProfileName,
                VersionLabel = BuildAssistantVersioningService.BuildVersionLabel(session),
                ProductName = session.ProductName,
                OutputPath = finalOutputPath,
                BuildSizeBytes = buildSizeBytes,
                Added = session.Added,
                Removed = session.Removed,
                Modified = session.Modified,
                KnownBugs = session.KnownBugs,
                LanguageCode = settings.ChangelogLanguage,
                Translations = BuildAssistantChangelogService.GetConfiguredTranslations(settings, session),
                WasSuccessful = wasSuccessful
            };

            settings.RecentBuildHistory.Insert(0, entry);
            while (settings.RecentBuildHistory.Count > settings.MaxHistoryEntries)
            {
                settings.RecentBuildHistory.RemoveAt(settings.RecentBuildHistory.Count - 1);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            WriteBuildLogFiles(session, entry);
        }

        #endregion

        #region Internal Logic

        private static void WriteBuildLogFiles(BuildAssistantSessionState session, BuildAssistantHistoryEntry entry)
        {
            if (session == null || entry == null)
            {
                return;
            }

            string outputFolder = BuildAssistantPathUtility.NormalizeSystemPath(session.OutputFolder);
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                return;
            }

            Directory.CreateDirectory(outputFolder);

            string latestLogPath = Path.Combine(outputFolder, LatestBuildLogFileName);
            string cumulativeLogPath = Path.Combine(outputFolder, CumulativeBuildLogFileName);

            string latestLogContents = BuildLogEntryText(session, entry);
            if (File.Exists(latestLogPath))
            {
                File.Delete(latestLogPath);
            }

            File.WriteAllText(latestLogPath, latestLogContents, Encoding.UTF8);

            string cumulativeLogContents = BuildLogEntryText(session, entry) + "\n\n";
            if (!File.Exists(cumulativeLogPath) || new FileInfo(cumulativeLogPath).Length == 0)
            {
                File.WriteAllText(cumulativeLogPath, cumulativeLogContents, Encoding.UTF8);
                return;
            }

            File.AppendAllText(cumulativeLogPath, cumulativeLogContents, Encoding.UTF8);
        }

        private static string BuildLogEntryText(BuildAssistantSessionState session, BuildAssistantHistoryEntry entry)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Timestamp: {entry.Timestamp}");
            builder.AppendLine($"Status: {(entry.WasSuccessful ? "Success" : "Failed")}");
            builder.AppendLine($"Platform: {entry.Platform}");
            builder.AppendLine($"Build Profile: {(string.IsNullOrWhiteSpace(entry.BuildProfileName) ? "-" : entry.BuildProfileName)}");
            builder.AppendLine($"Version: {entry.VersionLabel}");
            builder.AppendLine($"Product: {entry.ProductName}");
            builder.AppendLine($"Output: {entry.OutputPath}");
            builder.AppendLine($"Size: {FormatSize(entry.BuildSizeBytes)}");
            builder.AppendLine("Added:");
            builder.AppendLine(string.IsNullOrWhiteSpace(entry.Added) ? "-" : entry.Added);
            builder.AppendLine("Removed:");
            builder.AppendLine(string.IsNullOrWhiteSpace(entry.Removed) ? "-" : entry.Removed);
            builder.AppendLine("Modified:");
            builder.AppendLine(string.IsNullOrWhiteSpace(entry.Modified) ? "-" : entry.Modified);
            builder.AppendLine("Known Bugs:");
            builder.AppendLine(string.IsNullOrWhiteSpace(entry.KnownBugs) ? "-" : entry.KnownBugs);
            if (entry.Translations != null)
            {
                foreach (BuildAssistantChangelogNotes notes in entry.Translations)
                {
                    builder.AppendLine($"[{notes.LanguageCode}] Added:");
                    builder.AppendLine(string.IsNullOrWhiteSpace(notes.Added) ? "-" : notes.Added);
                    builder.AppendLine($"[{notes.LanguageCode}] Removed:");
                    builder.AppendLine(string.IsNullOrWhiteSpace(notes.Removed) ? "-" : notes.Removed);
                    builder.AppendLine($"[{notes.LanguageCode}] Modified:");
                    builder.AppendLine(string.IsNullOrWhiteSpace(notes.Modified) ? "-" : notes.Modified);
                    builder.AppendLine($"[{notes.LanguageCode}] Known Bugs:");
                    builder.AppendLine(string.IsNullOrWhiteSpace(notes.KnownBugs) ? "-" : notes.KnownBugs);
                }
            }

            AppendScriptChanges(builder, session != null ? session.ScriptChangeReport : null);
            builder.AppendLine("------------------------------------------------------------");
            return builder.ToString().TrimEnd();
        }


        private static string FormatSize(long sizeBytes)
        {
            if (sizeBytes <= 0)
            {
                return "Unknown";
            }

            double sizeInMb = sizeBytes / (1024d * 1024d);
            if (sizeInMb < 1024d)
            {
                return $"{sizeInMb:F2} MB";
            }

            double sizeInGb = sizeInMb / 1024d;
            return $"{sizeInGb:F2} GB";
        }

        private static void AppendScriptChanges(StringBuilder builder, BuildAssistantScriptChangeReport report)
        {
            builder.AppendLine("Script Changes:");
            builder.AppendLine(BuildAssistantScriptChangeService.BuildSummaryText(report));
            AppendScriptList(builder, "Added Scripts", report != null ? report.AddedScripts : null);
            AppendScriptList(builder, "Modified Scripts", report != null ? report.ModifiedScripts : null);
            AppendScriptList(builder, "Removed Scripts", report != null ? report.RemovedScripts : null);
        }

        private static void AppendScriptList(StringBuilder builder, string title, System.Collections.Generic.List<string> scripts)
        {
            builder.AppendLine(title + ":");
            if (scripts == null || scripts.Count == 0)
            {
                builder.AppendLine("- None");
                return;
            }

            for (int i = 0; i < scripts.Count; i++)
            {
                builder.AppendLine("- " + scripts[i]);
            }
        }

        #endregion
    }
}
