using System;
using System.Collections.Generic;

namespace Iung.BuildAssistant.Models
{
    [Serializable]
    public sealed class BuildAssistantScriptSnapshot
    {
        #region Fields

        public string CreatedAt = string.Empty;
        public List<BuildAssistantScriptSnapshotEntry> Scripts = new();

        #endregion
    }

    [Serializable]
    public sealed class BuildAssistantScriptSnapshotEntry
    {
        #region Fields

        public string Path = string.Empty;
        public string Hash = string.Empty;
        public long Size;
        public string LastWriteTimeUtc = string.Empty;

        #endregion
    }
}
