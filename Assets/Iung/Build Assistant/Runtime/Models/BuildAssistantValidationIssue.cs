using System;

namespace Iung.BuildAssistant.Models
{
    [Serializable]
    public sealed class BuildAssistantValidationIssue
    {
        #region Fields

        public BuildAssistantIssueSeverity Severity;
        public string Title;
        public string Message;
        public BuildAssistantStep SourceStep;

        #endregion

        #region Initialization

        public BuildAssistantValidationIssue(
            BuildAssistantIssueSeverity severity,
            string title,
            string message,
            BuildAssistantStep sourceStep)
        {
            Severity = severity;
            Title = title;
            Message = message;
            SourceStep = sourceStep;
        }

        #endregion
    }
}
