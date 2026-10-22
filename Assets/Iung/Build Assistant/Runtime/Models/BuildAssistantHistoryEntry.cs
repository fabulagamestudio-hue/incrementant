using System;
using System.Collections.Generic;

namespace Iung.BuildAssistant.Models
{
    [Serializable]
    public sealed class BuildAssistantHistoryEntry
    {
        #region Fields

        public string Timestamp;
        public string Platform;
        public string Profile;
        /// <summary>Unity Build Profile the build ran with; empty for entries recorded before profiles were tracked.</summary>
        public string BuildProfileId;
        public string BuildProfileName;
        public string VersionLabel;
        public string ProductName;
        public string OutputPath;
        public long BuildSizeBytes = -1;
        public string Added;
        public string Removed;
        public string Modified;
        public string KnownBugs;
        public string LanguageCode;
        public List<BuildAssistantChangelogNotes> Translations = new();
        public bool WasSuccessful;

        #endregion
    }
}
