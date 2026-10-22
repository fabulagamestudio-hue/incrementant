using UnityEditor;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    internal static class BuildAssistantEditorAssetLoader
    {
        public static void LoadWindowThumb(out Sprite thumbSprite, out Texture2D thumbTexture, out Rect thumbRect)
        {
            thumbSprite = null;
            thumbTexture = null;
            thumbRect = Rect.zero;

            string[] guids = AssetDatabase.FindAssets("Thumb t:Texture2D");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                thumbTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (thumbTexture == null)
                {
                    continue;
                }

                thumbRect = new Rect(0f, 0f, thumbTexture.width, thumbTexture.height);
                return;
            }
        }
    }
}
