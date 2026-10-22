using System;
using System.Collections.Generic;
using System.Linq;

namespace Iung.BuildAssistant.Models
{
    [Serializable]
    public sealed class BuildAssistantReviewReport
    {
        #region Fields

        public readonly List<BuildAssistantValidationIssue> Issues = new();

        #endregion

        #region Properties

        public bool HasBlockingIssues => Issues.Any(issue => issue.Severity == BuildAssistantIssueSeverity.Error);
        public int ErrorCount => Issues.Count(issue => issue.Severity == BuildAssistantIssueSeverity.Error);
        public int WarningCount => Issues.Count(issue => issue.Severity == BuildAssistantIssueSeverity.Warning);
        public int InfoCount => Issues.Count(issue => issue.Severity == BuildAssistantIssueSeverity.Info);

        #endregion
    }
}
