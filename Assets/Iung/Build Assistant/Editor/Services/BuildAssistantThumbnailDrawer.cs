using UnityEditor;
using UnityEngine;

namespace Iung.BuildAssistant.Services
{
    internal static class BuildAssistantThumbnailDrawer
    {
        public static bool TryDrawWindowThumb(Texture2D texture, Sprite sprite, Rect sourceRect, Color backgroundColor)
        {
            if (texture == null)
            {
                return false;
            }

            Rect bannerRect = GUILayoutUtility.GetRect(0f, 180f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bannerRect, backgroundColor);
            GUI.DrawTexture(bannerRect, texture, ScaleMode.StretchToFill, true);
            return true;
        }
    }
}
