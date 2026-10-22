using System.IO;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using UnityEditor;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantValidationService
    {
        #region Public API

        public static BuildAssistantReviewReport BuildReport(BuildAssistantSessionState session, BuildAssistantSettings settings)
        {
            BuildAssistantReviewReport report = new BuildAssistantReviewReport();
            if (session == null)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Missing session",
                    "The build session state could not be created.",
                    BuildAssistantStep.Context));
                return report;
            }

            ValidateContext(session, report);
            ValidateIdentity(session, report);
            ValidateRuntime(session, report);
            ValidateVersioning(session, report);
            ValidateChangelogTranslations(session, settings, report);
            ValidateProjectBuildSetup(report);
            ValidateAssetFields(session, report);
            ValidateSettings(settings, report);

            return report;
        }

        #endregion

        #region Internal Logic

        private static void ValidateContext(BuildAssistantSessionState session, BuildAssistantReviewReport report)
        {
            if (!BuildAssistantBuildProfileService.IsSessionForActiveProfile(session))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Build profile changed",
                    $"This session was prepared for '{session.BuildProfileName}', but Unity's active build profile is '{BuildAssistantBuildProfileService.GetActiveProfileName()}'. Reopen the Build Assistant window to load its configuration.",
                    BuildAssistantStep.Context));
            }

            if (string.IsNullOrWhiteSpace(session.OutputFolder))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Missing output folder",
                    "Select an output folder before starting the build.",
                    BuildAssistantStep.Context));
                return;
            }

            if (!Directory.Exists(session.OutputFolder))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Warning,
                    "Output folder does not exist yet",
                    "The selected output folder will be created when the build starts.",
                    BuildAssistantStep.Context));
            }

            string previewOutputPath = BuildAssistantVersioningService.BuildOutputPath(session);
            if (string.IsNullOrWhiteSpace(previewOutputPath))
            {
                return;
            }

            string previewOutputDirectory = Path.GetDirectoryName(previewOutputPath);
            bool targetAlreadyExists = File.Exists(previewOutputPath)
                                      || (!string.IsNullOrWhiteSpace(previewOutputDirectory) && Directory.Exists(previewOutputDirectory));
            if (targetAlreadyExists && !session.OverwriteExistingBuild && !session.AutoVersionIfTargetExists)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Existing build conflict",
                    "The target build folder already exists. Enable overwrite or automatic versioning to continue.",
                    BuildAssistantStep.Context));
            }

            if (targetAlreadyExists && session.OverwriteExistingBuild)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Warning,
                    "Folder overwrite warning",
                    "The target build folder already exists and will be modified or replaced because overwrite is enabled.",
                    BuildAssistantStep.Review));
            }
        }

        private static void ValidateIdentity(BuildAssistantSessionState session, BuildAssistantReviewReport report)
        {
            if (string.IsNullOrWhiteSpace(session.CompanyName))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Missing company name",
                    "Company name is required to keep Player Settings consistent.",
                    BuildAssistantStep.Identity));
            }

            if (string.IsNullOrWhiteSpace(session.ProductName))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Missing product name",
                    "Product name is required.",
                    BuildAssistantStep.Identity));
            }

            if (string.IsNullOrWhiteSpace(session.ExecutableName))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Warning,
                    "Executable name will mirror product name",
                    "Executable name was left empty, so the product name will be reused.",
                    BuildAssistantStep.Identity));
            }
        }

        private static void ValidateRuntime(BuildAssistantSessionState session, BuildAssistantReviewReport report)
        {
            if (session.DefaultScreenWidth <= 0 || session.DefaultScreenHeight <= 0)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Invalid resolution",
                    "Default screen width and height must be greater than zero.",
                    BuildAssistantStep.Runtime));
            }
        }

        private static void ValidateVersioning(BuildAssistantSessionState session, BuildAssistantReviewReport report)
        {
            if (session.VersioningMode == BuildAssistantVersioningMode.Numeric &&
                (session.MajorVersion < 0 || session.MinorVersion < 0 || session.PatchVersion < 0))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Invalid numeric version",
                    "Version numbers cannot be negative.",
                    BuildAssistantStep.Versioning));
            }

            string playerSettingsVersion = BuildAssistantVersioningService.BuildPlayerSettingsVersion(session);
            if (!BuildAssistantVersioningService.IsAppleSafePlayerSettingsVersion(playerSettingsVersion))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "Invalid Player Settings version",
                    "The Player Settings version must use the Apple-safe Major.Minor.Patch format with only numbers and dots.",
                    BuildAssistantStep.Versioning));
            }

            if (string.IsNullOrWhiteSpace(session.Added)
                && string.IsNullOrWhiteSpace(session.Removed)
                && string.IsNullOrWhiteSpace(session.Modified)
                && string.IsNullOrWhiteSpace(session.KnownBugs))
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Warning,
                    "Empty changelog",
                    "No changelog notes were provided for this build.",
                    BuildAssistantStep.Versioning));
            }
        }

        /// <summary>A language without notes falls back to the main notes at runtime; warn so it is a choice, not an oversight.</summary>
        private static void ValidateChangelogTranslations(BuildAssistantSessionState session, BuildAssistantSettings settings, BuildAssistantReviewReport report)
        {
            if (settings == null)
            {
                return;
            }

            bool hasMainNotes = !string.IsNullOrWhiteSpace(session.Added) || !string.IsNullOrWhiteSpace(session.Removed) ||
                                !string.IsNullOrWhiteSpace(session.Modified) || !string.IsNullOrWhiteSpace(session.KnownBugs);
            foreach (string languageCode in settings.ChangelogTranslationLanguages)
            {
                if (string.IsNullOrWhiteSpace(languageCode))
                {
                    continue;
                }

                BuildAssistantChangelogNotes notes = BuildAssistantChangelogService.FindExact(session.ChangelogTranslations, languageCode);
                bool hasTranslation = notes != null && notes.HasNotes;
                if (hasMainNotes && !hasTranslation)
                {
                    report.Issues.Add(new BuildAssistantValidationIssue(
                        BuildAssistantIssueSeverity.Warning,
                        $"Changelog without {languageCode.Trim()} notes",
                        $"Players using {languageCode.Trim()} will see the {settings.ChangelogLanguage} notes of this build.",
                        BuildAssistantStep.Versioning));
                }
            }
        }

        private static void ValidateProjectBuildSetup(BuildAssistantReviewReport report)
        {
            if (EditorBuildSettings.scenes == null || EditorBuildSettings.scenes.Length == 0)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "No scenes configured",
                    "Add at least one enabled scene to Build Settings before compiling.",
                    BuildAssistantStep.Review));
                return;
            }

            bool hasEnabledScene = false;
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (EditorBuildSettings.scenes[i].enabled)
                {
                    hasEnabledScene = true;
                    break;
                }
            }

            if (!hasEnabledScene)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Error,
                    "No enabled scenes",
                    "All build scenes are currently disabled.",
                    BuildAssistantStep.Review));
            }
        }

        private static void ValidateAssetFields(BuildAssistantSessionState session, BuildAssistantReviewReport report)
        {
            if (session.AppIcon == null)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Warning,
                    "Missing app icon",
                    "No app icon was assigned for this build.",
                    BuildAssistantStep.Identity));
            }

            report.Issues.Add(new BuildAssistantValidationIssue(
                BuildAssistantIssueSeverity.Info,
                "Splash configured in Unity Player Settings",
                "Use the Identity step button to open Project Settings > Player and configure Splash Image directly in Unity.",
                BuildAssistantStep.Identity));
        }

        private static void ValidateSettings(BuildAssistantSettings settings, BuildAssistantReviewReport report)
        {
            if (settings == null)
            {
                report.Issues.Add(new BuildAssistantValidationIssue(
                    BuildAssistantIssueSeverity.Warning,
                    "Settings asset unavailable",
                    "The settings asset could not be loaded. Defaults will still work for the current session.",
                    BuildAssistantStep.Context));
            }
        }

        #endregion
    }
}
