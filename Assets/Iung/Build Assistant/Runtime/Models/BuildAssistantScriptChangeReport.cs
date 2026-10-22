using System;
using System.Collections.Generic;

namespace Iung.BuildAssistant.Models
{
    [Serializable]
    public sealed class BuildAssistantScriptChangeReport
    {
        #region Fields

        public List<string> AddedScripts = new();
        public List<string> ModifiedScripts = new();
        public List<string> RemovedScripts = new();
        public bool HasPreviousSnapshot;
        public string PreviousSnapshotPath = string.Empty;
        public string LastRefreshTime = string.Empty;

        #endregion

        #region Properties

        public int AddedCount => AddedScripts != null ? AddedScripts.Count : 0;
        public int ModifiedCount => ModifiedScripts != null ? ModifiedScripts.Count : 0;
        public int RemovedCount => RemovedScripts != null ? RemovedScripts.Count : 0;
        public bool HasAnyChange => AddedCount > 0 || ModifiedCount > 0 || RemovedCount > 0;

        #endregion
    }
}
