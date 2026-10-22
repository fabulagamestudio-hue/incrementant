using System.IO;
using System.Collections.Generic;

namespace Iung.BuildAssistant.Services
{
    public static class BuildAssistantPathUtility
    {
        #region Public API

        public static string NormalizeAssetPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            string normalizedPath = rawPath.Replace('\\', '/').Trim();
            return CollapseDotSegments(normalizedPath);
        }

        public static string NormalizeSystemPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            string normalizedPath = rawPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).Trim();
            return normalizedPath;
        }

        public static bool IsAssetsPath(string path)
        {
            string normalizedPath = NormalizeAssetPath(path);
            return normalizedPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalizedPath, "Assets", System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPackagesPath(string path)
        {
            string normalizedPath = NormalizeAssetPath(path);
            return normalizedPath.StartsWith("Packages/", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalizedPath, "Packages", System.StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Internals

        private static string CollapseDotSegments(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string[] segments = path.Split('/');
            var resultSegments = new List<string>(segments.Length);

            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];

                if (string.IsNullOrEmpty(segment) || segment == ".")
                {
                    if (i == 0 && path.StartsWith("/"))
                    {
                        resultSegments.Add(string.Empty);
                    }

                    continue;
                }

                if (segment == "..")
                {
                    if (resultSegments.Count > 0 && resultSegments[resultSegments.Count - 1] != ".." && resultSegments[resultSegments.Count - 1] != string.Empty)
                    {
                        resultSegments.RemoveAt(resultSegments.Count - 1);
                    }
                    else
                    {
                        resultSegments.Add(segment);
                    }

                    continue;
                }

                resultSegments.Add(segment);
            }

            if (resultSegments.Count == 0)
            {
                return string.Empty;
            }

            if (resultSegments.Count == 1 && resultSegments[0] == string.Empty)
            {
                return "/";
            }

            return string.Join("/", resultSegments);
        }

        #endregion
    }
}
