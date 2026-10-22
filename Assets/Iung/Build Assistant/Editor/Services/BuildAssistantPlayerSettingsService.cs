using System;
using Iung.BuildAssistant.Models;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantPlayerSettingsService
    {
        #region Public API

        public static void SyncSessionFromPlayerSettings(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return;
            }

            session.CompanyName = PlayerSettings.companyName;
            session.ProductName = PlayerSettings.productName;
            session.ExecutableName = string.IsNullOrWhiteSpace(session.ExecutableName)
                ? PlayerSettings.productName
                : session.ExecutableName;
            session.RunInBackground = PlayerSettings.runInBackground;
            session.FullScreenMode = PlayerSettings.fullScreenMode;
            session.DefaultScreenWidth = PlayerSettings.defaultScreenWidth;
            session.DefaultScreenHeight = PlayerSettings.defaultScreenHeight;
            session.ResizableWindow = PlayerSettings.resizableWindow;
            session.AppIcon = ResolveCurrentIcon(session.Platform);
        }

        public static void ApplySessionToPlayerSettings(BuildAssistantSessionState session)
        {
            if (session == null)
            {
                return;
            }

            PlayerSettings.companyName = session.CompanyName;
            PlayerSettings.productName = session.ProductName;
            PlayerSettings.runInBackground = session.RunInBackground;
            PlayerSettings.fullScreenMode = session.FullScreenMode;
            PlayerSettings.defaultScreenWidth = session.DefaultScreenWidth;
            PlayerSettings.defaultScreenHeight = session.DefaultScreenHeight;
            PlayerSettings.resizableWindow = session.ResizableWindow;
            ApplyIcon(session);
            AssetDatabase.SaveAssets();
        }

        public static void OpenSplashSettings()
        {
            SettingsService.OpenProjectSettings("Project/Player");
        }

        #endregion

        #region Internal Logic

        private static Texture2D ResolveCurrentIcon(BuildAssistantPlatform platform)
        {
            try
            {
                Texture2D[] icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
                if (icons == null)
                {
                    return null;
                }

                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] != null)
                    {
                        return icons[i];
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Build Assistant: failed to read the default Player Settings icon. {exception.Message}");
            }

            return null;
        }

        private static void ApplyIcon(BuildAssistantSessionState session)
        {
            if (session == null || session.AppIcon == null)
            {
                return;
            }

            try
            {
                Texture2D[] currentIcons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
                Texture2D[] iconsToApply = currentIcons == null || currentIcons.Length == 0
                    ? new[] { session.AppIcon }
                    : new Texture2D[currentIcons.Length];

                for (int i = 0; i < iconsToApply.Length; i++)
                {
                    iconsToApply[i] = session.AppIcon;
                }

                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, iconsToApply, IconKind.Application);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Build Assistant: failed to apply the default Player Settings icon. {exception.Message}");
            }
        }

        #endregion
    }
}
