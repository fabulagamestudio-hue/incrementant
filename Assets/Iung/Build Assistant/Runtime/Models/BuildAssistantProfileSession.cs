using System;

namespace Iung.BuildAssistant.Models
{
    /// <summary>
    /// Build Assistant configuration saved for one Unity Build Profile (or platform when no profile is active).
    /// </summary>
    [Serializable]
    public sealed class BuildAssistantProfileSession
    {
        public string ProfileId = string.Empty;
        public string ProfileName = string.Empty;
        public BuildAssistantSessionState Session = new();
    }
}
