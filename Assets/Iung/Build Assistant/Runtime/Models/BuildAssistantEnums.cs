namespace Iung.BuildAssistant.Models
{
    public enum BuildAssistantPlatform
    {
        Windows,
        Android
    }

    public enum BuildAssistantProfile
    {
        Development,
        Homologation,
        Release
    }

    public enum BuildAssistantVersioningMode
    {
        Numeric,
        DateTime
    }

    public enum BuildAssistantIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum BuildAssistantStep
    {
        Context = 0,
        Identity = 1,
        Runtime = 2,
        Versioning = 3,
        Review = 4,
        Progress = 5
    }
}
