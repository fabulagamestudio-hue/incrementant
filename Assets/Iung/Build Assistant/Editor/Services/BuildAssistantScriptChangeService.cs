using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Iung.BuildAssistant.Models;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantScriptChangeService
    {
        private const string ReportsFolderName = "BuildReports";
        private const string LastSnapshotFileName = "LastScriptSnapshot.json";
        private const string ScriptChangeReportPrefix = "ScriptChanges";

        #region Public API

        /// <summary>
        /// Compares the scripts with the last successful build of <paramref name="profileId"/>. A profile without
        /// its own snapshot yet compares against the shared snapshot saved before profiles existed.
        /// </summary>
        public static BuildAssistantScriptChangeReport RefreshReport(string profileId = "")
        {
            BuildAssistantScriptSnapshot currentSnapshot = CaptureCurrentSnapshot();
            BuildAssistantScriptSnapshot previousSnapshot = LoadPreviousSnapshot(profileId);
            BuildAssistantScriptChangeReport report = CompareSnapshots(previousSnapshot, currentSnapshot);
            report.LastRefreshTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            report.PreviousSnapshotPath = GetSnapshotPath(profileId);
            return report;
        }

        public static void SaveCurrentSnapshot(string profileId = "")
        {
            BuildAssistantScriptSnapshot snapshot = CaptureCurrentSnapshot();
            string snapshotPath = GetSnapshotPath(profileId);
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath) ?? GetReportsFolderPath());
            File.WriteAllText(snapshotPath, JsonUtility.ToJson(snapshot, true), Encoding.UTF8);
        }

        public static void WriteReport(BuildAssistantSessionState session, BuildAssistantScriptChangeReport report)
        {
            if (session == null || report == null)
            {
                return;
            }

            string reportsFolderPath = GetReportsFolderPath();
            Directory.CreateDirectory(reportsFolderPath);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string reportPath = Path.Combine(reportsFolderPath, $"{ScriptChangeReportPrefix}_{timestamp}.txt");
            File.WriteAllText(reportPath, BuildReportText(session, report), Encoding.UTF8);
        }

        public static string BuildSummaryText(BuildAssistantScriptChangeReport report)
        {
            if (report == null)
            {
                return "Added: 0 | Modified: 0 | Removed: 0";
            }

            return $"Added: {report.AddedCount} | Modified: {report.ModifiedCount} | Removed: {report.RemovedCount}";
        }

        #endregion

        #region Internal Logic

        private static BuildAssistantScriptSnapshot CaptureCurrentSnapshot()
        {
            BuildAssistantScriptSnapshot snapshot = new BuildAssistantScriptSnapshot
            {
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string projectFolderPath = GetProjectFolderPath();
            string assetsFolderPath = Application.dataPath;
            if (!Directory.Exists(assetsFolderPath))
            {
                return snapshot;
            }

            string[] scriptPaths = Directory.GetFiles(assetsFolderPath, "*.cs", SearchOption.AllDirectories);
            Array.Sort(scriptPaths, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < scriptPaths.Length; i++)
            {
                string scriptPath = scriptPaths[i];
                FileInfo fileInfo = new FileInfo(scriptPath);
                snapshot.Scripts.Add(new BuildAssistantScriptSnapshotEntry
                {
                    Path = NormalizeUnityPath(Path.GetRelativePath(projectFolderPath, scriptPath)),
                    Hash = ComputeFileHash(scriptPath),
                    Size = fileInfo.Length,
                    LastWriteTimeUtc = fileInfo.LastWriteTimeUtc.ToString("O")
                });
            }

            return snapshot;
        }

        private static BuildAssistantScriptSnapshot LoadPreviousSnapshot(string profileId)
        {
            string snapshotPath = GetSnapshotPath(profileId);
            if (!File.Exists(snapshotPath))
            {
                snapshotPath = GetSnapshotPath(string.Empty);
            }

            if (!File.Exists(snapshotPath))
            {
                return null;
            }

            string json = File.ReadAllText(snapshotPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<BuildAssistantScriptSnapshot>(json);
        }

        private static BuildAssistantScriptChangeReport CompareSnapshots(BuildAssistantScriptSnapshot previousSnapshot, BuildAssistantScriptSnapshot currentSnapshot)
        {
            BuildAssistantScriptChangeReport report = new BuildAssistantScriptChangeReport
            {
                HasPreviousSnapshot = previousSnapshot != null && previousSnapshot.Scripts != null && previousSnapshot.Scripts.Count > 0
            };

            if (currentSnapshot == null || currentSnapshot.Scripts == null)
            {
                return report;
            }

            Dictionary<string, BuildAssistantScriptSnapshotEntry> currentByPath = CreateLookup(currentSnapshot);
            Dictionary<string, BuildAssistantScriptSnapshotEntry> previousByPath = CreateLookup(previousSnapshot);

            for (int i = 0; i < currentSnapshot.Scripts.Count; i++)
            {
                BuildAssistantScriptSnapshotEntry currentEntry = currentSnapshot.Scripts[i];
                if (currentEntry == null || string.IsNullOrWhiteSpace(currentEntry.Path))
                {
                    continue;
                }

                if (!previousByPath.TryGetValue(currentEntry.Path, out BuildAssistantScriptSnapshotEntry previousEntry))
                {
                    report.AddedScripts.Add(currentEntry.Path);
                    continue;
                }

                if (!string.Equals(currentEntry.Hash, previousEntry.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    report.ModifiedScripts.Add(currentEntry.Path);
                }
            }

            if (previousSnapshot == null || previousSnapshot.Scripts == null)
            {
                return report;
            }

            for (int i = 0; i < previousSnapshot.Scripts.Count; i++)
            {
                BuildAssistantScriptSnapshotEntry previousEntry = previousSnapshot.Scripts[i];
                if (previousEntry == null || string.IsNullOrWhiteSpace(previousEntry.Path))
                {
                    continue;
                }

                if (!currentByPath.ContainsKey(previousEntry.Path))
                {
                    report.RemovedScripts.Add(previousEntry.Path);
                }
            }

            return report;
        }

        private static Dictionary<string, BuildAssistantScriptSnapshotEntry> CreateLookup(BuildAssistantScriptSnapshot snapshot)
        {
            Dictionary<string, BuildAssistantScriptSnapshotEntry> lookup = new Dictionary<string, BuildAssistantScriptSnapshotEntry>(StringComparer.OrdinalIgnoreCase);
            if (snapshot == null || snapshot.Scripts == null)
            {
                return lookup;
            }

            for (int i = 0; i < snapshot.Scripts.Count; i++)
            {
                BuildAssistantScriptSnapshotEntry entry = snapshot.Scripts[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Path))
                {
                    continue;
                }

                lookup[entry.Path] = entry;
            }

            return lookup;
        }

        private static string ComputeFileHash(string filePath)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(stream);
            StringBuilder builder = new StringBuilder(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        private static string BuildReportText(BuildAssistantSessionState session, BuildAssistantScriptChangeReport report)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Product: {session.ProductName}");
            builder.AppendLine($"Build Profile: {session.BuildProfileName}");
            builder.AppendLine($"Version: {BuildAssistantVersioningService.BuildVersionLabel(session)}");
            builder.AppendLine(BuildSummaryText(report));
            builder.AppendLine();
            AppendScriptList(builder, "Added Scripts", report.AddedScripts);
            AppendScriptList(builder, "Modified Scripts", report.ModifiedScripts);
            AppendScriptList(builder, "Removed Scripts", report.RemovedScripts);
            return builder.ToString().TrimEnd();
        }

        private static void AppendScriptList(StringBuilder builder, string title, List<string> scripts)
        {
            builder.AppendLine(title + ":");
            if (scripts == null || scripts.Count == 0)
            {
                builder.AppendLine("- None");
                builder.AppendLine();
                return;
            }

            for (int i = 0; i < scripts.Count; i++)
            {
                builder.AppendLine("- " + scripts[i]);
            }

            builder.AppendLine();
        }

        private static string GetSnapshotPath(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return Path.Combine(GetReportsFolderPath(), LastSnapshotFileName);
            }

            string fileName = $"{Path.GetFileNameWithoutExtension(LastSnapshotFileName)}_{BuildAssistantVersioningService.SanitizeFileNameSegment(profileId)}{Path.GetExtension(LastSnapshotFileName)}";
            return Path.Combine(GetReportsFolderPath(), fileName);
        }

        private static string GetReportsFolderPath()
        {
            return Path.Combine(GetProjectFolderPath(), ReportsFolderName);
        }

        private static string GetProjectFolderPath()
        {
            return Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
        }

        private static string NormalizeUnityPath(string path)
        {
            return path.Replace('\\', '/');
        }

        #endregion
    }
}
