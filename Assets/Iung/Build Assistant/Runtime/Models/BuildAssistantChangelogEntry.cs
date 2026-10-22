using System;
using System.Collections.Generic;

namespace Iung.BuildAssistant.Models
{
    /// <summary>
    /// Release notes of one version, shipped with the build so runtime UI can list every version.
    /// Unlike <see cref="BuildAssistantHistoryEntry"/>, it keeps only player-facing data (no paths or sizes), plus
    /// the values <see cref="BuildAssistantTmpVersionLabel"/> needs to render the version with its own template.
    /// </summary>
    [Serializable]
    public sealed class BuildAssistantChangelogEntry
    {
        #region Fields

        /// <summary>Build Assistant version label; identifies the version in the changelog.</summary>
        public string VersionLabel;
        /// <summary>Build time, format <c>yyyy-MM-dd HHmm</c> (same as the history).</summary>
        public string Timestamp;
        /// <summary>Numeric version parts; -1 when unknown (entries imported from a date-based label).</summary>
        public int MajorVersion = -1;
        public int MinorVersion = -1;
        public int PatchVersion = -1;
        public string Suffix;
        public string Profile;
        /// <summary>Unity Build Profile the version was built with; empty for entries recorded before profiles were tracked.</summary>
        public string BuildProfileId;
        public string BuildProfileName;
        public string Added;
        public string Removed;
        public string Modified;
        public string KnownBugs;
        /// <summary>Language of the notes above (main language of the Build Assistant when the build ran).</summary>
        public string LanguageCode;
        /// <summary>The same notes in other languages.</summary>
        public List<BuildAssistantChangelogNotes> Translations = new();

        #endregion

        #region Properties

        public bool HasNumericVersion => MajorVersion >= 0 && MinorVersion >= 0 && PatchVersion >= 0;

        #endregion

        #region Public API

        /// <summary>
        /// Notes to show for <paramref name="languageCode"/>: the translation in that language when it has notes,
        /// otherwise the main notes. The result is a copy whose <see cref="BuildAssistantChangelogNotes.LanguageCode"/>
        /// tells which language was picked.
        /// </summary>
        public BuildAssistantChangelogNotes GetNotes(string languageCode)
        {
            // An exact match with the main language wins over a translation of the same language (pt-PT vs pt-BR).
            bool isMainLanguage = string.Equals(languageCode?.Trim(), LanguageCode?.Trim(), StringComparison.OrdinalIgnoreCase);
            BuildAssistantChangelogNotes translation = isMainLanguage ? null : BuildAssistantChangelogNotes.Find(Translations, languageCode);
            if (translation != null)
            {
                return translation.Clone();
            }

            return new BuildAssistantChangelogNotes
            {
                LanguageCode = LanguageCode,
                Added = Added,
                Removed = Removed,
                Modified = Modified,
                KnownBugs = KnownBugs
            };
        }

        #endregion
    }
}
