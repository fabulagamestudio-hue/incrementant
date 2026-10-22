using System;
using System.Collections.Generic;

namespace Iung.BuildAssistant.Models
{
    public enum BuildAssistantFolderNameToken
    {
        Executable,
        Profile,
        Version
    }

    [Serializable]
    public sealed class BuildAssistantFolderNameTokenRule
    {
        public BuildAssistantFolderNameToken Token = BuildAssistantFolderNameToken.Executable;
        public bool IsVisible = true;
    }

    public static class BuildAssistantFolderNameCompositionUtility
    {
        private static readonly BuildAssistantFolderNameToken[] DefaultTokenOrder =
        {
            BuildAssistantFolderNameToken.Executable,
            BuildAssistantFolderNameToken.Version
        };

        public static List<BuildAssistantFolderNameTokenRule> CreateDefault()
        {
            return Normalize(null);
        }

        public static List<BuildAssistantFolderNameTokenRule> Normalize(IList<BuildAssistantFolderNameTokenRule> source)
        {
            var visibilityByToken = new Dictionary<BuildAssistantFolderNameToken, bool>(DefaultTokenOrder.Length);
            var orderedTokens = new List<BuildAssistantFolderNameToken>(DefaultTokenOrder.Length);

            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    BuildAssistantFolderNameTokenRule rule = source[i];
                    if (rule == null)
                    {
                        continue;
                    }

                    BuildAssistantFolderNameToken token = rule.Token;
                    if (token == BuildAssistantFolderNameToken.Profile)
                    {
                        continue;
                    }

                    if (!IsKnownToken(token))
                    {
                        continue;
                    }

                    if (!visibilityByToken.ContainsKey(token))
                    {
                        visibilityByToken[token] = rule.IsVisible;
                    }

                    if (!orderedTokens.Contains(token))
                    {
                        orderedTokens.Add(token);
                    }
                }
            }

            for (int i = 0; i < DefaultTokenOrder.Length; i++)
            {
                BuildAssistantFolderNameToken defaultToken = DefaultTokenOrder[i];
                if (!visibilityByToken.ContainsKey(defaultToken))
                {
                    visibilityByToken[defaultToken] = true;
                }

                if (!orderedTokens.Contains(defaultToken))
                {
                    orderedTokens.Add(defaultToken);
                }
            }

            var normalized = new List<BuildAssistantFolderNameTokenRule>(DefaultTokenOrder.Length);
            for (int i = 0; i < orderedTokens.Count; i++)
            {
                BuildAssistantFolderNameToken token = orderedTokens[i];
                normalized.Add(new BuildAssistantFolderNameTokenRule
                {
                    Token = token,
                    IsVisible = visibilityByToken[token]
                });
            }

            return normalized;
        }

        private static bool IsKnownToken(BuildAssistantFolderNameToken token)
        {
            for (int i = 0; i < DefaultTokenOrder.Length; i++)
            {
                if (DefaultTokenOrder[i] == token)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
