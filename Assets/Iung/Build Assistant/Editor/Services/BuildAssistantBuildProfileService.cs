using Iung.BuildAssistant.Models;
using UnityEditor;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace Iung.BuildAssistant.Services
{
    /// <summary>
    /// Identifies the Unity Build Profile the build will use. Build Assistant keeps one configuration per profile;
    /// with no profile active (classic platform), the platform itself acts as the profile.
    /// </summary>
    public static class BuildAssistantBuildProfileService
    {
        private const string PlatformIdPrefix = "platform_";

        #region Public API

        public static BuildTarget GetActiveBuildTarget()
        {
            return BuildAssistantActivePlatformService.GetActiveBuildTarget();
        }

        public static string GetActivePlatformLabel()
        {
            return BuildAssistantActivePlatformService.GetActivePlatformLabel();
        }

        public static bool SyncSessionFromActivePlatform(BuildAssistantSessionState session)
        {
            return BuildAssistantActivePlatformService.SyncSessionFromActivePlatform(session);
        }

        public static string GetActiveBuildFileExtension()
        {
            return BuildAssistantActivePlatformService.GetActiveBuildFileExtension();
        }

        /// <summary>Stable key of the active profile: the asset GUID, or <c>platform_&lt;target&gt;</c> without a profile.</summary>
        public static string GetActiveProfileId()
        {
#if UNITY_6000_0_OR_NEWER
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));
                if (!string.IsNullOrEmpty(guid))
                {
                    return guid;
                }
            }
#endif
            return PlatformIdPrefix + GetActiveBuildTarget();
        }

        /// <summary>Display name of the active profile (the asset name, or the platform without a profile).</summary>
        public static string GetActiveProfileName()
        {
#if UNITY_6000_0_OR_NEWER
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null)
            {
                return profile.name;
            }
#endif
            return GetActivePlatformLabel();
        }

        /// <summary>Copies the active profile identity into <paramref name="session"/>; returns true when it changed.</summary>
        public static bool ApplyActiveProfile(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return false;
            }

            string profileId = GetActiveProfileId();
            string profileName = GetActiveProfileName();
            bool changed = session.BuildProfileId != profileId || session.BuildProfileName != profileName;
            session.BuildProfileId = profileId;
            session.BuildProfileName = profileName;
            return changed;
        }

        /// <summary>True when <paramref name="session"/> was prepared for another profile than the active one.</summary>
        public static bool IsSessionForActiveProfile(BuildAssistantSessionState session)
        {
            return session != null && session.BuildProfileId == GetActiveProfileId();
        }

        #endregion
    }
}
