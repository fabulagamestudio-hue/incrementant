using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Iung.BuildAssistant.Models;
using UnityEditor;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantVersioningService
    {
        #region Public API

        public static string BuildVersionLabel(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            string baseVersion = session.VersioningMode == BuildAssistantVersioningMode.DateTime
                ? DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)
                : $"v{session.MajorVersion}.{session.MinorVersion}.{session.PatchVersion}";

            if (string.IsNullOrWhiteSpace(session.ManualVersionSuffix))
            {
                return baseVersion;
            }

            return $"{baseVersion}_{SanitizeFileNameSegment(session.ManualVersionSuffix)}";
        }

        public static string BuildPlayerSettingsVersion(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            if (session.VersioningMode == BuildAssistantVersioningMode.DateTime)
            {
                return DateTime.Now.ToString("yyyyMMdd.HHmm.0", CultureInfo.InvariantCulture);
            }

            return $"{Math.Max(0, session.MajorVersion)}.{Math.Max(0, session.MinorVersion)}.{Math.Max(0, session.PatchVersion)}";
        }

        public static bool IsAppleSafePlayerSettingsVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            string[] segments = version.Split('.');
            if (segments.Length != 3)
            {
                return false;
            }

            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    return false;
                }

                for (int j = 0; j < segments[i].Length; j++)
                {
                    if (!char.IsDigit(segments[i][j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static string BuildOutputFileName(BuildAssistantSessionState session)
        {
            string productName = string.IsNullOrWhiteSpace(session.ExecutableName)
                ? session.ProductName
                : session.ExecutableName;

            string safeProductName = SanitizeFileNameSegment(productName);
            string extension = BuildAssistantActivePlatformService.GetActiveBuildFileExtension();

            return $"{safeProductName}{extension}";
        }

        public static string BuildOutputDirectoryName(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            List<BuildAssistantFolderNameTokenRule> composition =
                BuildAssistantFolderNameCompositionUtility.Normalize(session.FolderNameComposition);
            var visibleSegments = new List<string>(composition.Count);

            for (int i = 0; i < composition.Count; i++)
            {
                BuildAssistantFolderNameTokenRule rule = composition[i];
                if (rule == null || !rule.IsVisible)
                {
                    continue;
                }

                string tokenValue = ResolveFolderTokenValue(session, rule.Token);
                if (!string.IsNullOrWhiteSpace(tokenValue))
                {
                    visibleSegments.Add(tokenValue);
                }
            }

            if (visibleSegments.Count == 0)
            {
                visibleSegments.Add(ResolveFolderTokenValue(session, BuildAssistantFolderNameToken.Executable));
            }

            return string.Join("_", visibleSegments);
        }

        public static string BuildOutputDirectoryPath(BuildAssistantSessionState session)
        {
            string outputFolder = BuildAssistantPathUtility.NormalizeSystemPath(session.OutputFolder);
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                return string.Empty;
            }

            return Path.Combine(outputFolder, BuildOutputDirectoryName(session));
        }

        public static string BuildOutputPath(BuildAssistantSessionState session)
        {
            string outputDirectory = BuildOutputDirectoryPath(session);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return string.Empty;
            }

            return Path.Combine(outputDirectory, BuildOutputFileName(session));
        }

        public static string GetFileExtension(BuildAssistantPlatform platform)
        {
            return platform switch
            {
                BuildAssistantPlatform.Android => ".apk",
                _ => ".exe"
            };
        }

        public static string SanitizeFileNameSegment(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return "Unnamed";
            }

            string sanitized = rawValue.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                sanitized = sanitized.Replace(invalidCharacters[i], '_');
            }

            return sanitized.Replace(' ', '_');
        }

        public static string ResolveFolderTokenValue(BuildAssistantSessionState session, BuildAssistantFolderNameToken token)
        {
            if (session == null)
            {
                return string.Empty;
            }

            return token switch
            {
                BuildAssistantFolderNameToken.Executable => SanitizeFileNameSegment(
                    string.IsNullOrWhiteSpace(session.ExecutableName) ? session.ProductName : session.ExecutableName),
                BuildAssistantFolderNameToken.Profile => SanitizeFileNameSegment(session.Profile.ToString()),
                BuildAssistantFolderNameToken.Version => BuildVersionLabel(session),
                _ => string.Empty
            };
        }

        #endregion
    }
}
