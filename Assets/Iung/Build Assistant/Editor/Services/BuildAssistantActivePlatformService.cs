using Iung.BuildAssistant.Models;
using UnityEditor;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantActivePlatformService
    {
        #region Public API

        public static BuildTarget GetActiveBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }

        public static string GetActivePlatformLabel()
        {
            return GetActiveBuildTarget().ToString();
        }

        public static bool SyncSessionFromActivePlatform(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return false;
            }

            BuildAssistantPlatform activePlatform = ResolveSessionPlatform(GetActiveBuildTarget());
            if (session.Platform == activePlatform)
            {
                return false;
            }

            session.Platform = activePlatform;
            return true;
        }

        public static string GetActiveBuildFileExtension()
        {
            BuildTarget buildTarget = GetActiveBuildTarget();
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return ".apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                default:
                    return string.Empty;
            }
        }

        #endregion

        #region Internal Logic

        private static BuildAssistantPlatform ResolveSessionPlatform(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.Android => BuildAssistantPlatform.Android,
                _ => BuildAssistantPlatform.Windows
            };
        }

        #endregion
    }
}
