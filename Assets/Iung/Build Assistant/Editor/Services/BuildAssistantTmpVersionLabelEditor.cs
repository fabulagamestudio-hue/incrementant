using UnityEditor;
using UnityEngine;

namespace Iung.BuildAssistant
{
    [UnityEditor.CustomEditor(typeof(BuildAssistantTmpVersionLabel))]
    public sealed class BuildAssistantTmpVersionLabelEditor : UnityEditor.Editor
    {
        #region Fields

        private const MessageType PreviewMessageType = MessageType.Info;
        private const MessageType ValidationMessageType = MessageType.Warning;

        #endregion

        #region Public API

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            DrawTemplateHelp();
            DrawTemplatePreview();
            DrawUpdateButton();
        }

        #endregion

        #region Internal Logic

        private void DrawTemplateHelp()
        {
            BuildAssistantTmpVersionLabel versionLabel = (BuildAssistantTmpVersionLabel)target;
            EditorGUILayout.HelpBox($"Available tokens: {versionLabel.BuildAvailableTokensText()}", PreviewMessageType);
        }

        private void DrawTemplatePreview()
        {
            BuildAssistantTmpVersionLabel versionLabel = (BuildAssistantTmpVersionLabel)target;
            bool isTemplateValid = versionLabel.TryBuildVersionText(out string previewText, out string validationMessage);

            if (!isTemplateValid)
            {
                EditorGUILayout.HelpBox(validationMessage, ValidationMessageType);
                previewText = versionLabel.BuildVersionText();
            }

            EditorGUILayout.HelpBox($"Preview: {previewText}", PreviewMessageType);
        }

        private void DrawUpdateButton()
        {
            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Update Text"))
                {
                    BuildAssistantTmpVersionLabel versionLabel = (BuildAssistantTmpVersionLabel)target;
                    versionLabel.ApplyVersion();
                    EditorUtility.SetDirty(versionLabel);
                }
            }
        }

        #endregion
    }
}
