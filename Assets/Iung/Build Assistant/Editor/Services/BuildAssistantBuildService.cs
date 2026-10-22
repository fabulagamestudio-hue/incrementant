using System;
using System.Collections.Generic;
using System.IO;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantBuildService
    {
        #region Public API

        public static bool ExecuteBuild(BuildAssistantSessionState session, BuildAssistantSettings settings)
        {
            if (session == null)
            {
                return false;
            }

            BuildAssistantSessionService.RefreshPreview(session);
            BuildAssistantReviewReport report = BuildAssistantValidationService.BuildReport(session, settings);
            if (report.HasBlockingIssues)
            {
                session.ProgressTitle = "Blocked";
                session.ProgressMessage = "The build cannot start while blocking issues are present.";
                session.TechnicalLog = BuildTechnicalLog(report);
                session.LastBuildSucceeded = false;
                return false;
            }

            string finalOutputPath = ResolveFinalOutputPath(session);
            List<BuildAssistantChangelogEntry> previousChangelog = null;

            session.BuildStarted = true;
            session.ProgressTitle = "Preparing";
            session.ProgressMessage = "Applying Player Settings, collecting build scenes and comparing script changes.";
            session.TechnicalLog = string.Empty;
            session.ScriptChangeReport = BuildAssistantScriptChangeService.RefreshReport(session.BuildProfileId);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(finalOutputPath) ?? session.OutputFolder);
                ApplyPlayerSettings(session);
                previousChangelog = PrepareRuntimeVersionState(settings, session);

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = CollectEnabledScenes(),
                    target = BuildAssistantActivePlatformService.GetActiveBuildTarget(),
                    locationPathName = finalOutputPath,
                    options = BuildOptions.None
                };

                session.ProgressTitle = "Compiling";
                session.ProgressMessage = $"Building {session.BuildProfileName} ({BuildAssistantActivePlatformService.GetActivePlatformLabel()}).";

                BuildReport buildReport;
                using (BuildAssistantBuildContext.EnterAssistantBuildScope())
                {
                    buildReport = BuildPipeline.BuildPlayer(options);
                }
                bool wasSuccessful = buildReport.summary.result == BuildResult.Succeeded;

                session.ProgressTitle = wasSuccessful ? "Success" : "Failed";
                session.ProgressMessage = wasSuccessful
                    ? $"Build generated successfully at '{finalOutputPath}'."
                    : "Unity reported a build failure. Check the technical log below.";
                session.TechnicalLog = BuildTechnicalLog(buildReport);
                session.LastBuildSucceeded = wasSuccessful;
                session.PreviewBuildPath = finalOutputPath;

                if (wasSuccessful)
                {
                    BuildAssistantScriptChangeService.WriteReport(session, session.ScriptChangeReport);
                    BuildAssistantScriptChangeService.SaveCurrentSnapshot(session.BuildProfileId);
                }
                else
                {
                    BuildAssistantChangelogService.Restore(settings, previousChangelog);
                }

                long buildSizeBytes = wasSuccessful ? ResolveBuildSizeBytes(buildReport, finalOutputPath) : -1;
                BuildAssistantHistoryService.Record(settings, session, finalOutputPath, wasSuccessful, buildSizeBytes);
                return wasSuccessful;
            }
            catch (System.Exception exception)
            {
                session.ProgressTitle = "Exception";
                session.ProgressMessage = exception.Message;
                session.TechnicalLog = exception.ToString();
                session.LastBuildSucceeded = false;
                BuildAssistantChangelogService.Restore(settings, previousChangelog);
                BuildAssistantHistoryService.Record(settings, session, finalOutputPath, false);
                return false;
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static void RevealBuildFolder(BuildAssistantSessionState session)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.PreviewBuildPath))
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(session.PreviewBuildPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            EditorUtility.RevealInFinder(directoryPath);
        }

        #endregion

        #region Internal Logic

        private static string ResolveFinalOutputPath(BuildAssistantSessionState session)
        {
            string initialPath = BuildAssistantVersioningService.BuildOutputPath(session);
            if (string.IsNullOrWhiteSpace(initialPath))
            {
                return string.Empty;
            }

            string initialDirectory = Path.GetDirectoryName(initialPath) ?? session.OutputFolder;
            bool targetAlreadyExists = File.Exists(initialPath) || Directory.Exists(initialDirectory);
            if (!targetAlreadyExists)
            {
                return initialPath;
            }

            if (session.OverwriteExistingBuild)
            {
                return initialPath;
            }

            if (!session.AutoVersionIfTargetExists)
            {
                return initialPath;
            }

            string parentDirectory = Path.GetDirectoryName(initialDirectory) ?? session.OutputFolder;
            string directoryName = Path.GetFileName(initialDirectory);
            string fileName = Path.GetFileName(initialPath);
            int suffixIndex = 1;

            while (true)
            {
                string candidateDirectory = Path.Combine(parentDirectory, $"{directoryName}_{suffixIndex:00}");
                string candidatePath = Path.Combine(candidateDirectory, fileName);
                if (!Directory.Exists(candidateDirectory) && !File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                suffixIndex++;
            }
        }

        private static void ApplyPlayerSettings(BuildAssistantSessionState session)
        {
            PlayerSettings.companyName = session.CompanyName;
            PlayerSettings.productName = session.ProductName;
            PlayerSettings.bundleVersion = BuildAssistantVersioningService.BuildPlayerSettingsVersion(session);
            PlayerSettings.runInBackground = session.RunInBackground;
            PlayerSettings.fullScreenMode = session.FullScreenMode;
            PlayerSettings.defaultScreenWidth = session.DefaultScreenWidth;
            PlayerSettings.defaultScreenHeight = session.DefaultScreenHeight;
            PlayerSettings.resizableWindow = session.ResizableWindow;
            ApplyIconForPlatform(session);
        }

        /// <summary>Writes the version and release notes the build carries. Returns the previous changelog for rollback.</summary>
        private static List<BuildAssistantChangelogEntry> PrepareRuntimeVersionState(BuildAssistantSettings settings, BuildAssistantSessionState session)
        {
            if (settings == null || session == null)
            {
                return null;
            }

            DateTime timestamp = DateTime.Now;
            settings.UpdateRuntimeVersionState(
                session,
                BuildAssistantVersioningService.BuildVersionLabel(session),
                timestamp);
            List<BuildAssistantChangelogEntry> previousChangelog = BuildAssistantChangelogService.PrepareForBuild(settings, session, timestamp);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return previousChangelog;
        }

        private static void ApplyIconForPlatform(BuildAssistantSessionState session)
        {
            if (session == null || session.AppIcon == null)
            {
                return;
            }

            try
            {
                Texture2D[] currentIcons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
                Texture2D[] iconsToApply;
                if (currentIcons == null || currentIcons.Length == 0)
                {
                    iconsToApply = new[] { session.AppIcon };
                }
                else
                {
                    iconsToApply = new Texture2D[currentIcons.Length];
                    for (int i = 0; i < iconsToApply.Length; i++)
                    {
                        iconsToApply[i] = session.AppIcon;
                    }
                }

                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, iconsToApply, IconKind.Application);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Build Assistant: failed to apply the default Player Settings icon. {exception.Message}");
            }
        }

        private static string[] CollectEnabledScenes()
        {
            List<string> enabledScenes = new List<string>();
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < buildScenes.Length; i++)
            {
                if (!buildScenes[i].enabled)
                {
                    continue;
                }

                enabledScenes.Add(buildScenes[i].path);
            }

            return enabledScenes.ToArray();
        }

        private static long ResolveBuildSizeBytes(BuildReport buildReport, string finalOutputPath)
        {
            if (buildReport != null && buildReport.summary.totalSize > 0)
            {
                return buildReport.summary.totalSize > (ulong)long.MaxValue ? long.MaxValue : (long)buildReport.summary.totalSize;
            }

            return CalculatePathSize(finalOutputPath);
        }

        private static long CalculatePathSize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return -1;
            }

            try
            {
                if (File.Exists(path))
                {
                    return new FileInfo(path).Length;
                }

                string directoryPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                {
                    return -1;
                }

                long totalBytes = 0;
                string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    totalBytes += new FileInfo(files[i]).Length;
                }

                return totalBytes;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Build Assistant: failed to calculate build size. {exception.Message}");
                return -1;
            }
        }

        private static string BuildTechnicalLog(BuildReport report)
        {
            if (report == null)
            {
                return string.Empty;
            }

            double totalSizeInMb = report.summary.totalSize / (1024d * 1024d);
            return $"Result: {report.summary.result}\n" +
                   $"Output: {report.summary.outputPath}\n" +
                   $"Warnings: {report.summary.totalWarnings}\n" +
                   $"Errors: {report.summary.totalErrors}\n" +
                   $"Size: {totalSizeInMb:F2} MB\n" +
                   $"Duration: {report.summary.totalTime}";
        }

        private static string BuildTechnicalLog(BuildAssistantReviewReport report)
        {
            if (report == null)
            {
                return string.Empty;
            }

            List<string> lines = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                BuildAssistantValidationIssue issue = report.Issues[i];
                lines.Add($"[{issue.Severity}] {issue.Title}: {issue.Message}");
            }

            return string.Join("\n", lines);
        }

        #endregion
    }

    internal static class BuildAssistantBuildContext
    {
        private static int assistantBuildScopeDepth;

        public static bool IsAssistantBuild => assistantBuildScopeDepth > 0;

        public static IDisposable EnterAssistantBuildScope()
        {
            assistantBuildScopeDepth++;
            return new AssistantBuildScope();
        }

        private sealed class AssistantBuildScope : IDisposable
        {
            private bool isDisposed;

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                assistantBuildScopeDepth = Math.Max(assistantBuildScopeDepth - 1, 0);
            }
        }
    }
}
