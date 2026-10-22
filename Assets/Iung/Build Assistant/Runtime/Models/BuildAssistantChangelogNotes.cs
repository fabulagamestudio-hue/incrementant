using System;
using System.Collections.Generic;

namespace Iung.BuildAssistant.Models
{
    /// <summary>
    /// Release notes of one version written in an extra language. The notes of the main language stay in the
    /// Added/Removed/Modified/KnownBugs fields of the owner (session, history or changelog entry).
    /// </summary>
    [Serializable]
    public sealed class BuildAssistantChangelogNotes
    {
        #region Fields

        /// <summary>Language code, e.g. <c>pt-BR</c>. Matches a runtime locale code exactly or by its language part.</summary>
        public string LanguageCode;
        public string Added;
        public string Removed;
        public string Modified;
        public string KnownBugs;

        #endregion

        #region Properties

        public bool HasNotes =>
            !string.IsNullOrWhiteSpace(Added) || !string.IsNullOrWhiteSpace(Removed) ||
            !string.IsNullOrWhiteSpace(Modified) || !string.IsNullOrWhiteSpace(KnownBugs);

        #endregion

        #region Public API

        public BuildAssistantChangelogNotes Clone()
        {
            return new BuildAssistantChangelogNotes
            {
                LanguageCode = LanguageCode,
                Added = Added,
                Removed = Removed,
                Modified = Modified,
                KnownBugs = KnownBugs
            };
        }

        /// <summary>Copies of the translations that have a language code, for storing them in another owner.</summary>
        public static List<BuildAssistantChangelogNotes> CloneAll(IEnumerable<BuildAssistantChangelogNotes> translations)
        {
            var copies = new List<BuildAssistantChangelogNotes>();
            if (translations == null)
            {
                return copies;
            }

            foreach (BuildAssistantChangelogNotes notes in translations)
            {
                if (notes != null && !string.IsNullOrWhiteSpace(notes.LanguageCode))
                {
                    copies.Add(notes.Clone());
                }
            }

            return copies;
        }

        /// <summary>
        /// Translation for <paramref name="languageCode"/>: exact code first (<c>pt-BR</c>), then the same language
        /// (<c>pt</c> matches <c>pt-BR</c> and the other way around). Null when there is none with notes.
        /// </summary>
        public static BuildAssistantChangelogNotes Find(IReadOnlyList<BuildAssistantChangelogNotes> translations, string languageCode)
        {
            if (translations == null || string.IsNullOrWhiteSpace(languageCode))
            {
                return null;
            }

            string code = languageCode.Trim();
            string language = LanguagePart(code);
            BuildAssistantChangelogNotes sameLanguage = null;
            foreach (BuildAssistantChangelogNotes notes in translations)
            {
                if (notes == null || !notes.HasNotes || string.IsNullOrWhiteSpace(notes.LanguageCode))
                {
                    continue;
                }

                string notesCode = notes.LanguageCode.Trim();
                if (string.Equals(notesCode, code, StringComparison.OrdinalIgnoreCase))
                {
                    return notes;
                }

                if (sameLanguage == null && string.Equals(LanguagePart(notesCode), language, StringComparison.OrdinalIgnoreCase))
                {
                    sameLanguage = notes;
                }
            }

            return sameLanguage;
        }

        private static string LanguagePart(string code)
        {
            int separator = code.IndexOfAny(new[] { '-', '_' });
            return separator > 0 ? code.Substring(0, separator) : code;
        }

        #endregion
    }
}
