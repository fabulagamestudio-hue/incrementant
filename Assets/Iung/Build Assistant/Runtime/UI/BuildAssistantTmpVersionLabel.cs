using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using UnityEngine;

namespace Iung.BuildAssistant
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class BuildAssistantTmpVersionLabel : MonoBehaviour
    {
        #region Fields

        private const string DefaultVersionTemplate = "{FullVersion}";
        private const string InvalidTemplateFallback = "{FullVersion} ({Profile})";
        private const string TimestampFormat = "yyyy-MM-dd HHmm";
        private const string DateOutputFormat = "yyyyMMdd";
        private const string TimeOutputFormat = "HHmm";

        private static readonly char[] EntrySeparators = { ' ', '.', '_', '-' };

        private static readonly Regex NamedTokenRegex = new(@"(?<!\{)\{([A-Za-z][A-Za-z0-9]*)\}(?!\})", RegexOptions.Compiled);

        private static readonly string[] AvailableTokenNames =
        {
            "FullVersion",
            "Profile",
            "Major",
            "Minor",
            "Patch",
            "Date",
            "Time",
            "Suffix"
        };

        [SerializeField]
        [Tooltip("Template used to render the app version. Supported named tokens: {FullVersion}, {Profile}, {Major}, {Minor}, {Patch}, {Date}, {Time}, {Suffix}. Numeric tokens {0} and {1} are kept as legacy aliases for full version and profile.")]
        private string versionTemplate = DefaultVersionTemplate;

        [SerializeField]
        [Tooltip("Optional target text. If empty, the TMP_Text on the same GameObject is used.")]
        private TMP_Text targetText;

        [SerializeField]
        [Tooltip("Optional Build Assistant settings asset used to resolve the selected profile.")]
        private BuildAssistantSettings settings;

        #endregion

        #region Unity Messages

        private void Reset()
        {
            targetText = GetComponent<TMP_Text>();
            ApplyVersion();
        }

        private void OnEnable()
        {
            ApplyVersion();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ApplyVersion();
            }
        }
#endif

        #endregion

        #region Public API

        [ContextMenu("Apply Build Version")]
        public void ApplyVersion()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }

            if (targetText == null)
            {
                return;
            }

            targetText.text = BuildVersionText();
        }

        public string BuildVersionText()
        {
            return TryBuildVersionText(out string versionText, out _) ? versionText : BuildFallbackVersionText();
        }

        /// <summary>
        /// Renders a changelog entry with this label's template, so version lists match the label on screen.
        /// Falls back to the entry's version label when the template is invalid.
        /// </summary>
        public string BuildVersionText(BuildAssistantChangelogEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            string resolvedTemplate = string.IsNullOrWhiteSpace(versionTemplate) ? DefaultVersionTemplate : versionTemplate;
            if (!TryFormatVersionText(resolvedTemplate, BuildTemplateContext(entry), out string versionText, out _))
            {
                return entry.VersionLabel ?? string.Empty;
            }

            // Older entries may miss a token (no suffix, no numeric version); drop the separators left dangling.
            versionText = versionText.Trim(EntrySeparators);
            return string.IsNullOrEmpty(versionText) ? entry.VersionLabel ?? string.Empty : versionText;
        }

        public bool TryBuildVersionText(out string versionText, out string validationMessage)
        {
            VersionTemplateContext context = BuildTemplateContext();
            string resolvedTemplate = string.IsNullOrWhiteSpace(versionTemplate) ? DefaultVersionTemplate : versionTemplate;

            return TryFormatVersionText(resolvedTemplate, context, out versionText, out validationMessage);
        }

        public string BuildAvailableTokensText()
        {
            return string.Join(", ", Array.ConvertAll(AvailableTokenNames, tokenName => $"{{{tokenName}}}")) +
                   ", {0}, {1}";
        }

        #endregion

        #region Internal Logic

        private string ResolveVersionLabel()
        {
            if (settings != null && !string.IsNullOrWhiteSpace(settings.RuntimeVersionLabel))
            {
                return settings.RuntimeVersionLabel;
            }

            if (TryGetLatestSuccessfulBuildEntry(out BuildAssistantHistoryEntry latestEntry) &&
                !string.IsNullOrWhiteSpace(latestEntry.VersionLabel))
            {
                return latestEntry.VersionLabel;
            }

            return Application.version;
        }

        private string ResolveProfileLabel()
        {
            if (settings != null && settings.LastSessionState != null)
            {
                return settings.LastSessionState.Profile.ToString();
            }

            if (TryGetLatestSuccessfulBuildEntry(out BuildAssistantHistoryEntry latestEntry) &&
                !string.IsNullOrWhiteSpace(latestEntry.Profile))
            {
                return latestEntry.Profile;
            }

            return Debug.isDebugBuild ? "Development" : "Homologation";
        }

        private bool TryGetLatestSuccessfulBuildEntry(out BuildAssistantHistoryEntry entry)
        {
            entry = null;
            if (settings == null || settings.RecentBuildHistory == null || settings.RecentBuildHistory.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < settings.RecentBuildHistory.Count; i++)
            {
                BuildAssistantHistoryEntry candidate = settings.RecentBuildHistory[i];
                if (candidate == null || !candidate.WasSuccessful)
                {
                    continue;
                }

                entry = candidate;
                return true;
            }

            return false;
        }

        private VersionTemplateContext BuildTemplateContext()
        {
            BuildAssistantSessionState session = settings != null ? settings.LastSessionState : null;
            TryGetLatestSuccessfulBuildEntry(out BuildAssistantHistoryEntry latestEntry);

            DateTime timestamp = ResolveBuildTimestamp(latestEntry);

            return new VersionTemplateContext
            {
                FullVersion = ResolveVersionLabel(),
                Profile = ResolveProfileLabel(),
                Major = session != null ? session.MajorVersion.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Minor = session != null ? session.MinorVersion.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Patch = session != null ? session.PatchVersion.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Date = timestamp.ToString(DateOutputFormat, CultureInfo.InvariantCulture),
                Time = timestamp.ToString(TimeOutputFormat, CultureInfo.InvariantCulture),
                Suffix = session != null ? session.ManualVersionSuffix.Trim() : string.Empty
            };
        }

        private VersionTemplateContext BuildTemplateContext(BuildAssistantChangelogEntry entry)
        {
            bool hasTimestamp = DateTime.TryParseExact(
                entry.Timestamp,
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime timestamp);

            return new VersionTemplateContext
            {
                FullVersion = entry.VersionLabel ?? string.Empty,
                Profile = string.IsNullOrWhiteSpace(entry.Profile) ? ResolveProfileLabel() : entry.Profile,
                Major = entry.HasNumericVersion ? entry.MajorVersion.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Minor = entry.HasNumericVersion ? entry.MinorVersion.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Patch = entry.HasNumericVersion ? entry.PatchVersion.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Date = hasTimestamp ? timestamp.ToString(DateOutputFormat, CultureInfo.InvariantCulture) : string.Empty,
                Time = hasTimestamp ? timestamp.ToString(TimeOutputFormat, CultureInfo.InvariantCulture) : string.Empty,
                Suffix = entry.Suffix?.Trim() ?? string.Empty
            };
        }

        private DateTime ResolveBuildTimestamp(BuildAssistantHistoryEntry latestEntry)
        {
            if (settings != null &&
                DateTime.TryParseExact(
                    settings.RuntimeVersionTimestamp,
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime runtimeTimestamp))
            {
                return runtimeTimestamp;
            }

            if (latestEntry != null &&
                DateTime.TryParseExact(
                    latestEntry.Timestamp,
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedTimestamp))
            {
                return parsedTimestamp;
            }

            return DateTime.Now;
        }

        private string BuildFallbackVersionText()
        {
            VersionTemplateContext context = BuildTemplateContext();
            TryFormatVersionText(InvalidTemplateFallback, context, out string fallbackText, out _);
            return fallbackText;
        }

        private static bool TryFormatVersionText(
            string template,
            VersionTemplateContext context,
            out string versionText,
            out string validationMessage)
        {
            validationMessage = string.Empty;
            versionText = string.Empty;

            string namedTokenValidationMessage = string.Empty;
            string namedTokenResult = NamedTokenRegex.Replace(template, match =>
            {
                string tokenName = match.Groups[1].Value;
                if (TryResolveNamedToken(context, tokenName, out string tokenValue))
                {
                    return tokenValue;
                }

                namedTokenValidationMessage = $"Unknown token: {{{tokenName}}}";
                return match.Value;
            });

            if (!string.IsNullOrEmpty(namedTokenValidationMessage))
            {
                validationMessage = namedTokenValidationMessage;
                return false;
            }

            try
            {
                versionText = string.Format(
                    CultureInfo.InvariantCulture,
                    namedTokenResult,
                    context.FullVersion,
                    context.Profile,
                    context.Major,
                    context.Minor,
                    context.Patch,
                    context.Date,
                    context.Time,
                    context.Suffix);
                return true;
            }
            catch (FormatException exception)
            {
                validationMessage = exception.Message;
                return false;
            }
        }

        private static bool TryResolveNamedToken(VersionTemplateContext context, string tokenName, out string tokenValue)
        {
            Dictionary<string, string> tokenValues = new(StringComparer.OrdinalIgnoreCase)
            {
                { "FullVersion", context.FullVersion },
                { "Profile", context.Profile },
                { "Major", context.Major },
                { "Minor", context.Minor },
                { "Patch", context.Patch },
                { "Date", context.Date },
                { "Time", context.Time },
                { "Suffix", context.Suffix }
            };

            return tokenValues.TryGetValue(tokenName, out tokenValue);
        }

        #endregion

        #region Types

        private sealed class VersionTemplateContext
        {
            public string FullVersion = string.Empty;
            public string Profile = string.Empty;
            public string Major = string.Empty;
            public string Minor = string.Empty;
            public string Patch = string.Empty;
            public string Date = string.Empty;
            public string Time = string.Empty;
            public string Suffix = string.Empty;
        }

        #endregion
    }
}
