using System.Collections.Generic;
using System.IO;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using Iung.BuildAssistant.Services;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Iung.BuildAssistant.Editor.Windows
{
    public sealed class BuildAssistantWindow : EditorWindow
    {
        private const string WindowIconFileName = "Icon.png";
        private const string WindowThumbFileName = "Thumb.png";
        private static readonly string[] WindowLogoFolderNames = { "Logos", "Logo" };
        private static readonly string[] WindowIconFallbackPaths =
        {
            "Assets/Project/Art/UI/Logos/Icon.png",
            "Assets/Project/Art/UI/IungLogo/Icon.png"
        };
        private static readonly string[] WindowThumbFallbackPaths =
        {
            "Assets/Iung/Build Assistant/UI/Thumb.png",
            "Assets/Project/Art/UI/Logos/Thumb.png",
            "Assets/Project/Art/UI/IungLogo/Thumb.png"
        };
        private static readonly Color WindowThumbBackgroundColor = new Color32(0x20, 0x0C, 0x3B, 0xFF);

        private static readonly string[] StepNames =
        {
            "1. Context",
            "2. Identity",
            "3. Runtime",
            "4. Versioning",
            "5. Review",
            "6. Progress"
        };

        private static readonly string[] ScriptChangeTabNames =
        {
            "Summary",
            "Added",
            "Modified",
            "Removed"
        };

        private static readonly string[] ChangelogTabNames =
        {
            "Added",
            "Modified",
            "Removed",
            "Known Bugs"
        };

        #region Fields

        private BuildAssistantSettings settings;
        private BuildAssistantSessionState session;
        private Vector2 mainScrollPosition;
        private BuildAssistantReviewReport cachedReviewReport;
        private Sprite windowThumbSprite;
        private Texture2D windowThumbTexture;
        private Rect windowThumbSourceRect;
        private ReorderableList folderNameComposerList;
        private bool isBuildQueued;
        private int scriptChangesTabIndex;
        private int changelogTabIndex;
        // 0 = main changelog language; 1..n = settings.ChangelogTranslationLanguages[n - 1].
        private int changelogLanguageIndex;
        private int activeChangelogEditTabIndex = -1;
        private int activeChangelogEditLanguageIndex = -1;
        private string activeChangelogDraft = string.Empty;
        private Vector2 scriptChangesScrollPosition;
        private Vector2 changelogPreviewScrollPosition;
        private Vector2 changelogEditScrollPosition;
        private string editLockMessage = string.Empty;
        private static readonly Color FolderComposerRowColorA = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color FolderComposerRowColorB = new Color(1f, 1f, 1f, 0.06f);

        #endregion

        #region Public API

        public static void OpenWindow()
        {
            BuildAssistantWindow window = GetWindow<BuildAssistantWindow>(title: "Build Assistant");
            window.ApplyWindowTitle();
            window.minSize = new Vector2(920f, 700f);
            window.Show();
        }

        #endregion

        #region Unity Messages

        private void OnEnable()
        {
            ApplyWindowTitle();
            BuildAssistantEditorAssetLoader.LoadWindowThumb(out windowThumbSprite, out windowThumbTexture, out windowThumbSourceRect);
            settings = BuildAssistantSettingsService.GetOrCreateSettings();
            session = BuildAssistantSessionService.CreateDefaultSession(settings);
            SetupFolderNameComposerList();
            BuildAssistantPlayerSettingsService.SyncSessionFromPlayerSettings(session);
            SyncSessionFromActivePlatform();
            BuildAssistantSessionService.RefreshPreview(session);
            RefreshReviewReport();
        }

        private void OnDisable()
        {
            BuildAssistantSessionService.PersistSession(settings, session);
        }

        private void OnGUI()
        {
            EnsureState();
            SyncSessionFromActiveProfile();
            SyncSessionFromActivePlatform();
            DrawWindowThumb();
            DrawToolbar();
            DrawStepHeader();

            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);

            EditorGUILayout.Space(6f);
            switch ((BuildAssistantStep)sessionStepIndex)
            {
                case BuildAssistantStep.Context:
                    DrawContextStep();
                    break;
                case BuildAssistantStep.Identity:
                    DrawIdentityStep();
                    break;
                case BuildAssistantStep.Runtime:
                    DrawRuntimeStep();
                    break;
                case BuildAssistantStep.Versioning:
                    DrawVersioningStep();
                    break;
                case BuildAssistantStep.Review:
                    DrawReviewStep();
                    break;
                case BuildAssistantStep.Progress:
                    DrawProgressStep();
                    break;
            }

            EditorGUILayout.EndScrollView();
            DrawBottomNavigation();
        }

        #endregion

        #region Properties

        private int sessionStepIndex
        {
            get => session != null ? Mathf.Clamp((int)currentStep, 0, StepNames.Length - 1) : 0;
            set
            {
                int nextStepIndex = Mathf.Clamp(value, 0, StepNames.Length - 1);
                if (HasActiveChangelogEdit() && nextStepIndex != (int)currentStep)
                {
                    editLockMessage = "Save or cancel the current changelog edit before changing tabs.";
                    Repaint();
                    return;
                }

                editLockMessage = string.Empty;
                currentStep = (BuildAssistantStep)nextStepIndex;
            }
        }

        private BuildAssistantStep currentStep = BuildAssistantStep.Context;

        #endregion

        #region Drawing

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reset Session", EditorStyles.toolbarButton))
            {
                session = BuildAssistantSessionService.CreateDefaultSession(settings, restorePersistedSession: false);
                SetupFolderNameComposerList();
                BuildAssistantSessionService.RefreshPreview(session);
                RefreshReviewReport();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
            {
                BuildAssistantSettingsService.SelectSettingsAsset();
            }

            if (GUILayout.Button("Refresh Review", EditorStyles.toolbarButton))
            {
                RefreshReviewReport();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                $"Build Profile: {BuildAssistantBuildProfileService.GetActiveProfileName()} | Platform: {BuildAssistantActivePlatformService.GetActivePlatformLabel()}",
                EditorStyles.miniLabel,
                GUILayout.Width(380f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWindowThumb()
        {
            if (windowThumbTexture == null)
            {
                return;
            }

            float bannerWidth = Mathf.Max(1f, position.width);
            float sourceWidth = windowThumbSourceRect.width > 0f ? windowThumbSourceRect.width : windowThumbTexture.width;
            float sourceHeight = windowThumbSourceRect.height > 0f ? windowThumbSourceRect.height : windowThumbTexture.height;
            float maxImageWidth = Mathf.Max(1f, bannerWidth - 12f);
            float imageScale = sourceWidth > 0f ? Mathf.Min(1f, maxImageWidth / sourceWidth) : 1f;
            const float imageDisplayScale = 0.55f;
            float imageWidth = Mathf.Max(1f, sourceWidth * imageScale * imageDisplayScale);
            float imageHeight = Mathf.Max(1f, sourceHeight * imageScale * imageDisplayScale);
            float bannerHeight = imageHeight;
            Rect layoutRect = GUILayoutUtility.GetRect(0f, bannerHeight, GUILayout.ExpandWidth(true));
            Rect bannerRect = new(0f, layoutRect.y, bannerWidth, bannerHeight);
            EditorGUI.DrawRect(bannerRect, WindowThumbBackgroundColor);
            Rect imageRect = new(
                bannerRect.x + (bannerRect.width - imageWidth) * 0.5f,
                bannerRect.y + (bannerRect.height - imageHeight) * 0.5f,
                imageWidth,
                imageHeight);

            if (windowThumbSprite != null)
            {
                Rect uv = new(
                    windowThumbSourceRect.x / windowThumbTexture.width,
                    windowThumbSourceRect.y / windowThumbTexture.height,
                    windowThumbSourceRect.width / windowThumbTexture.width,
                    windowThumbSourceRect.height / windowThumbTexture.height);
                GUI.DrawTextureWithTexCoords(imageRect, windowThumbTexture, uv, true);
            }
            else
            {
                GUI.DrawTexture(imageRect, windowThumbTexture, ScaleMode.StretchToFill, true);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawStepHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Build Flow", EditorStyles.boldLabel);
            sessionStepIndex = GUILayout.Toolbar(sessionStepIndex, StepNames);
            DrawEditLockMessage();
        }

        private void DrawContextStep()
        {
            EditorGUILayout.LabelField("Build Type and Destination", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The build platform comes from Unity's active Build Profile/Build Settings target. Every Build Profile keeps its own Build Assistant configuration " +
                "(output folder, identity, runtime, version, changelog, folder name and script snapshot): change the profile in Unity's Build Profiles window to edit another one.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Active Build Profile", BuildAssistantBuildProfileService.GetActiveProfileName());
                EditorGUILayout.TextField("Active Platform", BuildAssistantActivePlatformService.GetActivePlatformLabel());
            }

            EditorGUI.BeginChangeCheck();
            session.OutputFolder = EditorGUILayout.TextField("Output Folder", session.OutputFolder);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button("Browse...", GUILayout.MaxWidth(120f)))
            {
                string selectedFolder = EditorUtility.OpenFolderPanel("Build Output Folder", session.OutputFolder, string.Empty);
                if (!string.IsNullOrWhiteSpace(selectedFolder))
                {
                    session.OutputFolder = selectedFolder;
                    GUI.changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            session.OverwriteExistingBuild = EditorGUILayout.Toggle("Overwrite Existing", session.OverwriteExistingBuild);
            session.AutoVersionIfTargetExists = EditorGUILayout.Toggle("Auto Version If Exists", session.AutoVersionIfTargetExists);
            DrawPreviewBox();

            if (EditorGUI.EndChangeCheck())
            {
                HandleSessionChanged();
            }
        }

        private void DrawIdentityStep()
        {
            EditorGUILayout.LabelField("Application Identity", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These values define how the application is identified externally and how the output file is named.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            session.CompanyName = EditorGUILayout.TextField("Company Name", session.CompanyName);
            session.ProductName = EditorGUILayout.TextField("Product Name", session.ProductName);
            session.ExecutableName = EditorGUILayout.TextField("Executable Name", session.ExecutableName);
            session.AppIcon = (Texture2D)EditorGUILayout.ObjectField("Icon", session.AppIcon, typeof(Texture2D), false);

            EditorGUILayout.HelpBox(
                "Company name, product name and icon mirror Unity Player Settings. Splash Screen remains configured directly in Project Settings to avoid Unity version API instability.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button("Open Unity Splash Settings", GUILayout.MaxWidth(220f)))
            {
                BuildAssistantPlayerSettingsService.OpenSplashSettings();
            }
            EditorGUILayout.EndHorizontal();

            DrawPreviewBox();

            if (EditorGUI.EndChangeCheck())
            {
                HandleSessionChanged();
            }
        }

        private void DrawRuntimeStep()
        {
            EditorGUILayout.LabelField("Runtime Behavior", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure how the compiled application should behave on the target machine.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            session.RunInBackground = EditorGUILayout.Toggle("Run In Background", session.RunInBackground);
            session.FullScreenMode = (FullScreenMode)EditorGUILayout.EnumPopup("Full Screen Mode", session.FullScreenMode);
            session.DefaultScreenWidth = EditorGUILayout.IntField("Default Width", session.DefaultScreenWidth);
            session.DefaultScreenHeight = EditorGUILayout.IntField("Default Height", session.DefaultScreenHeight);
            session.ResizableWindow = EditorGUILayout.Toggle("Resizable Window", session.ResizableWindow);

            if (EditorGUI.EndChangeCheck())
            {
                HandleSessionChanged();
            }
        }

        private void DrawVersioningStep()
        {
            EditorGUILayout.LabelField("Versioning and Changelog", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Choose version strategy, review script changes since the last successful build and register what changed in the build.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            session.VersioningMode = (BuildAssistantVersioningMode)EditorGUILayout.EnumPopup("Versioning Mode", session.VersioningMode);
            if (session.VersioningMode == BuildAssistantVersioningMode.Numeric)
            {
                EditorGUILayout.BeginHorizontal();
                session.MajorVersion = EditorGUILayout.IntField("Major", session.MajorVersion);
                session.MinorVersion = EditorGUILayout.IntField("Minor", session.MinorVersion);
                session.PatchVersion = EditorGUILayout.IntField("Patch", session.PatchVersion);
                EditorGUILayout.EndHorizontal();
            }

            session.ManualVersionSuffix = EditorGUILayout.TextField("Suffix", session.ManualVersionSuffix);
            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginHorizontal();
            DrawScriptChangesPanel();
            GUILayout.Space(8f);
            DrawChangelogTabsPanel();
            EditorGUILayout.EndHorizontal();

            DrawPreviewBox();
            DrawHistoryBox();

            if (EditorGUI.EndChangeCheck())
            {
                HandleSessionChanged();
            }
        }

        private void DrawScriptChangesPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(Mathf.Max(300f, position.width * 0.42f)));
            EditorGUILayout.LabelField("Script Changes", EditorStyles.boldLabel);

            if (session.ScriptChangeReport == null)
            {
                session.ScriptChangeReport = BuildAssistantScriptChangeService.RefreshReport();
            }

            EditorGUILayout.LabelField(BuildAssistantScriptChangeService.BuildSummaryText(session.ScriptChangeReport), EditorStyles.miniBoldLabel);
            if (!session.ScriptChangeReport.HasPreviousSnapshot)
            {
                EditorGUILayout.HelpBox("No previous successful build snapshot was found. The next successful build will become the first comparison reference.", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Script Changes"))
            {
                session.ScriptChangeReport = BuildAssistantScriptChangeService.RefreshReport();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            scriptChangesTabIndex = GUILayout.Toolbar(scriptChangesTabIndex, ScriptChangeTabNames);
            scriptChangesScrollPosition = EditorGUILayout.BeginScrollView(scriptChangesScrollPosition, GUILayout.MinHeight(220f), GUILayout.MaxHeight(320f));

            switch (scriptChangesTabIndex)
            {
                case 1:
                    DrawScriptPathList(session.ScriptChangeReport.AddedScripts, "No added scripts detected.");
                    break;
                case 2:
                    DrawScriptPathList(session.ScriptChangeReport.ModifiedScripts, "No modified scripts detected.");
                    break;
                case 3:
                    DrawScriptPathList(session.ScriptChangeReport.RemovedScripts, "No removed scripts detected.");
                    break;
                default:
                    DrawScriptChangeSummary(session.ScriptChangeReport);
                    break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawChangelogTabsPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Changelog", EditorStyles.boldLabel);

            string[] languageNames = GetChangelogLanguageNames();
            changelogLanguageIndex = Mathf.Clamp(changelogLanguageIndex, 0, languageNames.Length - 1);
            if (languageNames.Length > 1)
            {
                int requestedLanguageIndex = GUILayout.Toolbar(changelogLanguageIndex, languageNames);
                if (requestedLanguageIndex != changelogLanguageIndex)
                {
                    if (HasActiveChangelogEdit())
                    {
                        editLockMessage = "Save or cancel the current changelog edit before changing the changelog language.";
                        Repaint();
                    }
                    else
                    {
                        editLockMessage = string.Empty;
                        changelogLanguageIndex = requestedLanguageIndex;
                        changelogPreviewScrollPosition = Vector2.zero;
                        changelogEditScrollPosition = Vector2.zero;
                    }
                }
            }

            int requestedTabIndex = GUILayout.Toolbar(changelogTabIndex, ChangelogTabNames);
            if (requestedTabIndex != changelogTabIndex)
            {
                if (HasActiveChangelogEdit())
                {
                    editLockMessage = "Save or cancel the current changelog edit before changing changelog tabs.";
                    Repaint();
                }
                else
                {
                    editLockMessage = string.Empty;
                    changelogTabIndex = requestedTabIndex;
                    changelogPreviewScrollPosition = Vector2.zero;
                    changelogEditScrollPosition = Vector2.zero;
                }
            }

            DrawChangelogTabContent(changelogTabIndex);
            EditorGUILayout.EndVertical();
        }

        private void DrawChangelogTabContent(int tabIndex)
        {
            string label = GetChangelogLabel(tabIndex);
            bool isEditing = activeChangelogEditTabIndex == tabIndex && activeChangelogEditLanguageIndex == changelogLanguageIndex;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            bool shouldStopDrawingThisFrame = false;
            if (isEditing)
            {
                if (GUILayout.Button("Save", GUILayout.Width(72f)))
                {
                    SaveActiveChangelogEdit();
                    shouldStopDrawingThisFrame = true;
                }

                if (!shouldStopDrawingThisFrame && GUILayout.Button("Cancel", GUILayout.Width(72f)))
                {
                    CancelActiveChangelogEdit();
                    shouldStopDrawingThisFrame = true;
                }
            }
            else if (GUILayout.Button("Edit", GUILayout.Width(72f)))
            {
                StartChangelogEdit(tabIndex);
                shouldStopDrawingThisFrame = true;
            }

            EditorGUILayout.EndHorizontal();

            if (shouldStopDrawingThisFrame)
            {
                return;
            }

            if (isEditing)
            {
                GUI.SetNextControlName(GetChangelogControlName(tabIndex));
                changelogEditScrollPosition = EditorGUILayout.BeginScrollView(changelogEditScrollPosition, GUILayout.MinHeight(240f));
                activeChangelogDraft = EditorGUILayout.TextArea(activeChangelogDraft ?? string.Empty, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox("Use Save to confirm or Cancel to discard. Navigation and build execution stay locked while this edit is active.", MessageType.Info);
            }
            else
            {
                string currentValue = GetChangelogText(tabIndex);
                using (new EditorGUI.DisabledScope(true))
                {
                    changelogPreviewScrollPosition = EditorGUILayout.BeginScrollView(changelogPreviewScrollPosition, GUILayout.MinHeight(240f));
                    EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(currentValue) ? "No entries registered." : currentValue, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private static void DrawScriptChangeSummary(BuildAssistantScriptChangeReport report)
        {
            if (report == null)
            {
                EditorGUILayout.LabelField("No script change information available.", EditorStyles.wordWrappedLabel);
                return;
            }

            EditorGUILayout.LabelField("Added Scripts", report.AddedCount.ToString());
            EditorGUILayout.LabelField("Modified Scripts", report.ModifiedCount.ToString());
            EditorGUILayout.LabelField("Removed Scripts", report.RemovedCount.ToString());
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Last Refresh", string.IsNullOrWhiteSpace(report.LastRefreshTime) ? "-" : report.LastRefreshTime);
            EditorGUILayout.LabelField("Snapshot", string.IsNullOrWhiteSpace(report.PreviousSnapshotPath) ? "-" : report.PreviousSnapshotPath, EditorStyles.wordWrappedLabel);
        }

        private static void DrawScriptPathList(List<string> scriptPaths, string emptyMessage)
        {
            if (scriptPaths == null || scriptPaths.Count == 0)
            {
                EditorGUILayout.LabelField(emptyMessage, EditorStyles.wordWrappedLabel);
                return;
            }

            for (int i = 0; i < scriptPaths.Count; i++)
            {
                EditorGUILayout.LabelField(scriptPaths[i], EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawReviewStep()
        {
            EditorGUILayout.LabelField("Final Review", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Review blocking issues, warnings and the final build summary before compiling.", MessageType.Info);

            DrawSummaryBox();
            DrawFolderNameComposerBox();
            DrawIssuesBox();

            EditorGUILayout.Space(8f);
            bool hasActiveEdit = HasActiveChangelogEdit();
            using (new EditorGUI.DisabledScope(hasActiveEdit || (cachedReviewReport != null && cachedReviewReport.HasBlockingIssues)))
            {
                if (GUILayout.Button("Start Build", GUILayout.Height(32f)))
                {
                    QueueBuildExecution();
                }
            }

            if (hasActiveEdit)
            {
                EditorGUILayout.HelpBox("Save or cancel the current changelog edit before starting a build.", MessageType.Warning);
            }

            if (cachedReviewReport != null && cachedReviewReport.HasBlockingIssues)
            {
                EditorGUILayout.HelpBox("Resolve all blocking issues before starting the build.", MessageType.Error);
            }
        }

        private void DrawProgressStep()
        {
            EditorGUILayout.LabelField("Progress and Result", EditorStyles.boldLabel);

            MessageType messageType = session.LastBuildSucceeded ? MessageType.Info : MessageType.Warning;
            if (!session.BuildStarted)
            {
                messageType = MessageType.Info;
            }

            EditorGUILayout.HelpBox(session.ProgressMessage, messageType);
            EditorGUILayout.LabelField("Status", session.ProgressTitle);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Output", string.IsNullOrWhiteSpace(session.PreviewBuildPath) ? "-" : session.PreviewBuildPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Technical Log", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(session.TechnicalLog) ? "No build has been executed yet." : session.TechnicalLog, GUILayout.MinHeight(180f));

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(session.PreviewBuildPath)))
            {
                if (GUILayout.Button("Open Build Folder"))
                {
                    BuildAssistantBuildService.RevealBuildFolder(session);
                }
            }

            if (GUILayout.Button("Back To Review"))
            {
                sessionStepIndex = (int)BuildAssistantStep.Review;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewBox()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Build Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("File Name", string.IsNullOrWhiteSpace(session.PreviewBuildName) ? "-" : session.PreviewBuildName, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Full Path", string.IsNullOrWhiteSpace(session.PreviewBuildPath) ? "-" : session.PreviewBuildPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawHistoryBox()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Recent History ({session.BuildProfileName})", EditorStyles.boldLabel);

            int displayedEntries = 0;
            for (int i = 0; settings != null && i < settings.RecentBuildHistory.Count && displayedEntries < 5; i++)
            {
                BuildAssistantHistoryEntry entry = settings.RecentBuildHistory[i];
                if (!BelongsToActiveProfile(entry))
                {
                    continue;
                }

                displayedEntries++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{entry.Timestamp} | {entry.Platform}", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"{entry.ProductName} | {entry.VersionLabel}", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(entry.OutputPath, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Size", entry.BuildSizeBytes > 0 ? FormatSize(entry.BuildSizeBytes) : "Unknown", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }

            if (displayedEntries == 0)
            {
                EditorGUILayout.LabelField("No build history recorded for this build profile yet.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSummaryBox()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Build Profile", BuildAssistantBuildProfileService.GetActiveProfileName());
            EditorGUILayout.LabelField("Active Platform", BuildAssistantActivePlatformService.GetActivePlatformLabel());
            EditorGUILayout.LabelField("Company", session.CompanyName);
            EditorGUILayout.LabelField("Product", session.ProductName);

            if (IsFolderTokenVisible(BuildAssistantFolderNameToken.Executable))
            {
                EditorGUILayout.LabelField("Executable", BuildAssistantVersioningService.ResolveFolderTokenValue(session, BuildAssistantFolderNameToken.Executable));
            }


            if (IsFolderTokenVisible(BuildAssistantFolderNameToken.Version))
            {
                EditorGUILayout.LabelField("Version", BuildAssistantVersioningService.ResolveFolderTokenValue(session, BuildAssistantFolderNameToken.Version));
            }

            EditorGUILayout.LabelField("Folder Name", BuildAssistantVersioningService.BuildOutputDirectoryName(session), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Output", session.PreviewBuildPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Estimated Build Size", ResolveEstimatedBuildSizeLabel(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Available Space", ResolveAvailableSpaceLabel(), EditorStyles.wordWrappedLabel);

            if (WillOverwriteBuildFolder())
            {
                EditorGUILayout.HelpBox(
                    "Folder overwrite warning: the final build folder already exists and will be modified or replaced because Overwrite Existing is enabled.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }


        private string ResolveEstimatedBuildSizeLabel()
        {
            long estimatedSizeBytes = CalculateEstimatedBuildSizeBytes();
            if (estimatedSizeBytes <= 0)
            {
                return "No previous build size available.";
            }

            return FormatSize(estimatedSizeBytes);
        }

        private string ResolveAvailableSpaceLabel()
        {
            long availableSpaceBytes = CalculateAvailableSpaceBytes(session != null ? session.OutputFolder : string.Empty);
            if (availableSpaceBytes < 0)
            {
                return "Unavailable for the selected output folder.";
            }

            return FormatSize(availableSpaceBytes);
        }

        private long CalculateEstimatedBuildSizeBytes()
        {
            if (settings == null || settings.RecentBuildHistory == null || settings.RecentBuildHistory.Count == 0)
            {
                return -1;
            }

            for (int i = 0; i < settings.RecentBuildHistory.Count; i++)
            {
                BuildAssistantHistoryEntry entry = settings.RecentBuildHistory[i];
                if (entry == null || !entry.WasSuccessful || !BelongsToActiveProfile(entry))
                {
                    continue;
                }

                long entrySizeBytes = entry.BuildSizeBytes > 0
                    ? entry.BuildSizeBytes
                    : CalculatePathSizeBytes(entry.OutputPath);

                if (entrySizeBytes > 0)
                {
                    return entrySizeBytes;
                }
            }

            return -1;
        }

        private bool WillOverwriteBuildFolder()
        {
            if (session == null || !session.OverwriteExistingBuild)
            {
                return false;
            }

            string outputPath = BuildAssistantVersioningService.BuildOutputPath(session);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return false;
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            return !string.IsNullOrWhiteSpace(outputDirectory) && Directory.Exists(outputDirectory);
        }

        private static long CalculateAvailableSpaceBytes(string outputFolder)
        {
            string existingPath = ResolveExistingPath(outputFolder);
            if (string.IsNullOrWhiteSpace(existingPath))
            {
                return -1;
            }

            try
            {
                DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(existingPath));
                return driveInfo.AvailableFreeSpace;
            }
            catch
            {
                return -1;
            }
        }

        private static string ResolveExistingPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalizedPath = BuildAssistantPathUtility.NormalizeSystemPath(path);
            try
            {
                normalizedPath = Path.GetFullPath(normalizedPath);
            }
            catch
            {
                return string.Empty;
            }

            if (File.Exists(normalizedPath))
            {
                return normalizedPath;
            }

            DirectoryInfo directoryInfo = Directory.Exists(normalizedPath)
                ? new DirectoryInfo(normalizedPath)
                : Directory.GetParent(normalizedPath);

            while (directoryInfo != null && !directoryInfo.Exists)
            {
                directoryInfo = directoryInfo.Parent;
            }

            return directoryInfo != null ? directoryInfo.FullName : string.Empty;
        }

        private static long CalculatePathSizeBytes(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return -1;
            }

            try
            {
                if (File.Exists(path))
                {
                    return new FileInfo(path).Length;
                }

                string directoryPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                {
                    return -1;
                }

                long totalSizeBytes = 0;
                string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    totalSizeBytes += new FileInfo(files[i]).Length;
                }

                return totalSizeBytes;
            }
            catch
            {
                return -1;
            }
        }

        private static string FormatSize(long sizeBytes)
        {
            if (sizeBytes <= 0)
            {
                return "-";
            }

            double sizeInMb = sizeBytes / (1024d * 1024d);
            if (sizeInMb < 1024d)
            {
                return $"{sizeInMb:F2} MB";
            }

            double sizeInGb = sizeInMb / 1024d;
            return $"{sizeInGb:F2} GB";
        }

        private void DrawFolderNameComposerBox()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Folder Name Composer (Kanban)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag cards by the handle (|||) to reorder them. Visibility changes the real folder name composition.", MessageType.None);

            SetupFolderNameComposerList();
            if (folderNameComposerList != null)
            {
                folderNameComposerList.DoLayoutList();
            }

            EditorGUILayout.LabelField("Folder Preview", BuildAssistantVersioningService.BuildOutputDirectoryName(session), EditorStyles.wordWrappedLabel);
            if (!HasAnyVisibleToken())
            {
                EditorGUILayout.HelpBox("All tokens are hidden. Fallback applied: EXECUTABLE.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawIssuesBox()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            if (cachedReviewReport == null || cachedReviewReport.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(
                $"Errors: {cachedReviewReport.ErrorCount} | Warnings: {cachedReviewReport.WarningCount} | Info: {cachedReviewReport.InfoCount}",
                EditorStyles.miniBoldLabel);

            for (int i = 0; i < cachedReviewReport.Issues.Count; i++)
            {
                BuildAssistantValidationIssue issue = cachedReviewReport.Issues[i];
                MessageType messageType = ResolveMessageType(issue.Severity);
                EditorGUILayout.HelpBox($"[{issue.SourceStep}] {issue.Title}\n{issue.Message}", messageType);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Quick Edit", EditorStyles.boldLabel);
            DrawQuickEditButtons();
            EditorGUILayout.EndVertical();
        }

        private void DrawQuickEditButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Context"))
            {
                sessionStepIndex = (int)BuildAssistantStep.Context;
            }

            if (GUILayout.Button("Identity"))
            {
                sessionStepIndex = (int)BuildAssistantStep.Identity;
            }

            if (GUILayout.Button("Runtime"))
            {
                sessionStepIndex = (int)BuildAssistantStep.Runtime;
            }

            if (GUILayout.Button("Versioning"))
            {
                sessionStepIndex = (int)BuildAssistantStep.Versioning;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBottomNavigation()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            using (new EditorGUI.DisabledScope(sessionStepIndex <= 0))
            {
                if (GUILayout.Button("Previous", GUILayout.Height(28f)))
                {
                    sessionStepIndex--;
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(sessionStepIndex >= StepNames.Length - 1))
            {
                if (GUILayout.Button("Next", GUILayout.Height(28f), GUILayout.Width(120f)))
                {
                    if (sessionStepIndex == (int)BuildAssistantStep.Review)
                    {
                        sessionStepIndex = (int)BuildAssistantStep.Progress;
                    }
                    else
                    {
                        sessionStepIndex++;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private bool HasActiveChangelogEdit()
        {
            return activeChangelogEditTabIndex >= 0;
        }

        private void DrawEditLockMessage()
        {
            if (string.IsNullOrWhiteSpace(editLockMessage))
            {
                return;
            }

            EditorGUILayout.HelpBox(editLockMessage, MessageType.Warning);
        }

        private string GetChangelogLabel(int tabIndex)
        {
            return tabIndex >= 0 && tabIndex < ChangelogTabNames.Length ? ChangelogTabNames[tabIndex] : ChangelogTabNames[0];
        }

        private string GetChangelogControlName(int tabIndex)
        {
            return $"BuildAssistant_Changelog_{tabIndex}";
        }

        private string[] GetChangelogLanguageNames()
        {
            var names = new List<string>();
            string mainLanguage = settings != null ? settings.ChangelogLanguage : string.Empty;
            names.Add(string.IsNullOrWhiteSpace(mainLanguage) ? "Main" : $"{mainLanguage} (main)");
            if (settings != null)
            {
                foreach (string languageCode in settings.ChangelogTranslationLanguages)
                {
                    names.Add(string.IsNullOrWhiteSpace(languageCode) ? "?" : languageCode.Trim());
                }
            }

            return names.ToArray();
        }

        /// <summary>Notes of the selected extra language, created in the session on first use; null for the main language.</summary>
        private BuildAssistantChangelogNotes GetTranslationNotes(int languageIndex, bool create)
        {
            if (languageIndex <= 0 || settings == null || languageIndex > settings.ChangelogTranslationLanguages.Count)
            {
                return null;
            }

            string languageCode = settings.ChangelogTranslationLanguages[languageIndex - 1];
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return null;
            }

            session.ChangelogTranslations ??= new List<BuildAssistantChangelogNotes>();
            BuildAssistantChangelogNotes notes = BuildAssistantChangelogService.FindExact(session.ChangelogTranslations, languageCode);
            if (notes == null && create)
            {
                notes = new BuildAssistantChangelogNotes { LanguageCode = languageCode.Trim() };
                session.ChangelogTranslations.Add(notes);
            }

            return notes;
        }

        private string GetChangelogText(int tabIndex)
        {
            return GetChangelogText(changelogLanguageIndex, tabIndex);
        }

        private string GetChangelogText(int languageIndex, int tabIndex)
        {
            if (languageIndex > 0)
            {
                BuildAssistantChangelogNotes notes = GetTranslationNotes(languageIndex, false);
                return notes == null ? string.Empty : tabIndex switch
                {
                    1 => notes.Modified,
                    2 => notes.Removed,
                    3 => notes.KnownBugs,
                    _ => notes.Added
                } ?? string.Empty;
            }

            return tabIndex switch
            {
                1 => session.Modified,
                2 => session.Removed,
                3 => session.KnownBugs,
                _ => session.Added
            } ?? string.Empty;
        }

        private void SetChangelogText(int languageIndex, int tabIndex, string value)
        {
            if (languageIndex > 0)
            {
                BuildAssistantChangelogNotes notes = GetTranslationNotes(languageIndex, true);
                if (notes == null)
                {
                    return;
                }

                switch (tabIndex)
                {
                    case 1:
                        notes.Modified = value ?? string.Empty;
                        break;
                    case 2:
                        notes.Removed = value ?? string.Empty;
                        break;
                    case 3:
                        notes.KnownBugs = value ?? string.Empty;
                        break;
                    default:
                        notes.Added = value ?? string.Empty;
                        break;
                }

                return;
            }

            switch (tabIndex)
            {
                case 1:
                    session.Modified = value ?? string.Empty;
                    break;
                case 2:
                    session.Removed = value ?? string.Empty;
                    break;
                case 3:
                    session.KnownBugs = value ?? string.Empty;
                    break;
                default:
                    session.Added = value ?? string.Empty;
                    break;
            }
        }

        private void StartChangelogEdit(int tabIndex)
        {
            if (HasActiveChangelogEdit())
            {
                editLockMessage = "Save or cancel the current changelog edit before starting another edit.";
                Repaint();
                return;
            }

            editLockMessage = string.Empty;
            activeChangelogEditTabIndex = tabIndex;
            activeChangelogEditLanguageIndex = changelogLanguageIndex;
            activeChangelogDraft = GetChangelogText(tabIndex);
            changelogEditScrollPosition = Vector2.zero;
            GUI.FocusControl(GetChangelogControlName(tabIndex));
        }

        private void SaveActiveChangelogEdit()
        {
            if (activeChangelogEditTabIndex < 0)
            {
                return;
            }

            SetChangelogText(activeChangelogEditLanguageIndex, activeChangelogEditTabIndex, activeChangelogDraft);
            activeChangelogEditTabIndex = -1;
            activeChangelogEditLanguageIndex = -1;
            activeChangelogDraft = string.Empty;
            editLockMessage = string.Empty;
            ClearTextInputFocus();
            HandleSessionChanged();
        }

        private void CancelActiveChangelogEdit()
        {
            if (activeChangelogEditTabIndex < 0)
            {
                return;
            }

            activeChangelogEditTabIndex = -1;
            activeChangelogEditLanguageIndex = -1;
            activeChangelogDraft = string.Empty;
            editLockMessage = string.Empty;
            ClearTextInputFocus();
            Repaint();
        }

        private static void ClearTextInputFocus()
        {
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;
        }

        #endregion

        #region Internal Logic

        private void EnsureState()
        {
            if (settings == null)
            {
                settings = BuildAssistantSettingsService.GetOrCreateSettings();
            }

            if (session == null)
            {
                session = BuildAssistantSessionService.CreateDefaultSession(settings);
                SetupFolderNameComposerList();
            }

            if (cachedReviewReport == null)
            {
                RefreshReviewReport();
            }

            if (folderNameComposerList == null)
            {
                SetupFolderNameComposerList();
            }
        }


        /// <summary>History entries of the session's Build Profile; entries recorded before profiles were tracked count for every profile.</summary>
        private bool BelongsToActiveProfile(BuildAssistantHistoryEntry entry)
        {
            return entry != null &&
                   (string.IsNullOrEmpty(entry.BuildProfileId) || session == null || entry.BuildProfileId == session.BuildProfileId);
        }

        /// <summary>
        /// Loads the configuration of the Build Profile that became active in Unity: the current one is saved first,
        /// then the session, composer and review are rebuilt for the new profile.
        /// </summary>
        private void SyncSessionFromActiveProfile()
        {
            if (session == null || BuildAssistantBuildProfileService.IsSessionForActiveProfile(session))
            {
                return;
            }

            activeChangelogEditTabIndex = -1;
            activeChangelogEditLanguageIndex = -1;
            activeChangelogDraft = string.Empty;
            editLockMessage = string.Empty;
            ClearTextInputFocus();

            BuildAssistantSessionService.PersistSession(settings, session);
            session = BuildAssistantSessionService.CreateDefaultSession(settings);
            folderNameComposerList = null;
            SetupFolderNameComposerList();
            BuildAssistantPlayerSettingsService.SyncSessionFromPlayerSettings(session);
            BuildAssistantActivePlatformService.SyncSessionFromActivePlatform(session);
            BuildAssistantSessionService.RefreshPreview(session);
            RefreshReviewReport();
            Repaint();
        }

        private void SyncSessionFromActivePlatform()
        {
            if (BuildAssistantActivePlatformService.SyncSessionFromActivePlatform(session))
            {
                BuildAssistantSessionService.RefreshPreview(session);
                RefreshReviewReport();
            }
        }

        private void HandleSessionChanged()
        {
            BuildAssistantPlayerSettingsService.ApplySessionToPlayerSettings(session);
            BuildAssistantSessionService.RefreshPreview(session);
            RefreshReviewReport();
            Repaint();
        }

        private void QueueBuildExecution()
        {
            if (isBuildQueued)
            {
                return;
            }

            isBuildQueued = true;
            sessionStepIndex = (int)BuildAssistantStep.Progress;
            Repaint();
            EditorApplication.delayCall += ExecuteQueuedBuild;
        }

        private void ExecuteQueuedBuild()
        {
            if (!isBuildQueued)
            {
                return;
            }

            isBuildQueued = false;
            BuildAssistantBuildService.ExecuteBuild(session, settings);
            RefreshReviewReport();
            Repaint();
        }

        private void RefreshReviewReport()
        {
            BuildAssistantSessionService.RefreshPreview(session);
            cachedReviewReport = BuildAssistantValidationService.BuildReport(session, settings);
        }

        private void SetupFolderNameComposerList()
        {
            if (session == null)
            {
                folderNameComposerList = null;
                return;
            }

            EnsureFolderNameCompositionInitialized();
            if (folderNameComposerList != null && ReferenceEquals(folderNameComposerList.list, session.FolderNameComposition))
            {
                return;
            }

            folderNameComposerList = new ReorderableList(session.FolderNameComposition, typeof(BuildAssistantFolderNameTokenRule), true, false, false, false)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 12f
            };

            folderNameComposerList.drawHeaderCallback = DrawFolderNameComposerHeader;
            folderNameComposerList.drawElementBackgroundCallback = DrawFolderNameComposerElementBackground;
            folderNameComposerList.drawElementCallback = DrawFolderNameComposerElement;
            folderNameComposerList.onReorderCallback = _ =>
            {
                PersistFolderNameComposerAsDefault();
                HandleSessionChanged();
            };
        }

        private void DrawFolderNameComposerHeader(Rect rect)
        {
            Rect cardsLabelRect = new Rect(rect.x + 30f, rect.y, rect.width * 0.55f, rect.height);
            Rect valueLabelRect = new Rect(rect.x + rect.width * 0.58f, rect.y, rect.width * 0.25f, rect.height);
            Rect visibleLabelRect = new Rect(rect.xMax - 95f, rect.y, 90f, rect.height);

            EditorGUI.LabelField(cardsLabelRect, "Card");
            EditorGUI.LabelField(valueLabelRect, "Preview");
            EditorGUI.LabelField(visibleLabelRect, "Visible");
        }

        private void DrawFolderNameComposerElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            Color fillColor = isActive
                ? new Color(0.23f, 0.33f, 0.47f, 0.35f)
                : (index % 2 == 0 ? FolderComposerRowColorA : FolderComposerRowColorB);
            EditorGUI.DrawRect(rect, fillColor);
        }

        private void DrawFolderNameComposerElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (session == null || session.FolderNameComposition == null || index < 0 || index >= session.FolderNameComposition.Count)
            {
                return;
            }

            BuildAssistantFolderNameTokenRule rule = session.FolderNameComposition[index];
            if (rule == null)
            {
                return;
            }

            Rect contentRect = new Rect(rect.x, rect.y + 3f, rect.width, EditorGUIUtility.singleLineHeight);
            Rect dragRect = new Rect(contentRect.x, contentRect.y, 26f, contentRect.height);
            Rect tokenRect = new Rect(contentRect.x + 30f, contentRect.y, contentRect.width * 0.53f, contentRect.height);
            Rect valueRect = new Rect(contentRect.x + contentRect.width * 0.58f, contentRect.y, contentRect.width * 0.25f, contentRect.height);
            Rect visibleRect = new Rect(contentRect.xMax - 95f, contentRect.y, 95f, contentRect.height);

            EditorGUI.LabelField(dragRect, "|||", EditorStyles.centeredGreyMiniLabel);
            EditorGUI.LabelField(tokenRect, rule.Token.ToString().ToUpperInvariant(), EditorStyles.boldLabel);
            EditorGUI.LabelField(valueRect, BuildAssistantVersioningService.ResolveFolderTokenValue(session, rule.Token), EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            bool visible = EditorGUI.ToggleLeft(visibleRect, "Visible", rule.IsVisible);
            if (EditorGUI.EndChangeCheck())
            {
                rule.IsVisible = visible;
                PersistFolderNameComposerAsDefault();
                HandleSessionChanged();
            }
        }

        private void PersistFolderNameComposerAsDefault()
        {
            if (settings == null || session == null)
            {
                return;
            }

            settings.UpdateDefaultFolderNameComposition(session.FolderNameComposition);
            BuildAssistantSettingsService.SaveSettings(settings);
        }

        private bool HasAnyVisibleToken()
        {
            if (session == null || session.FolderNameComposition == null)
            {
                return false;
            }

            for (int i = 0; i < session.FolderNameComposition.Count; i++)
            {
                BuildAssistantFolderNameTokenRule rule = session.FolderNameComposition[i];
                if (rule != null && rule.IsVisible)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFolderTokenVisible(BuildAssistantFolderNameToken token)
        {
            if (session == null)
            {
                return false;
            }

            EnsureFolderNameCompositionInitialized();
            for (int i = 0; i < session.FolderNameComposition.Count; i++)
            {
                BuildAssistantFolderNameTokenRule rule = session.FolderNameComposition[i];
                if (rule != null && rule.Token == token)
                {
                    return rule.IsVisible;
                }
            }

            return false;
        }

        private void EnsureFolderNameCompositionInitialized()
        {
            if (session == null)
            {
                return;
            }

            if (session.FolderNameComposition == null || session.FolderNameComposition.Count == 0)
            {
                session.FolderNameComposition = BuildAssistantFolderNameCompositionUtility.CreateDefault();
            }
        }

        private static MessageType ResolveMessageType(BuildAssistantIssueSeverity severity)
        {
            return severity switch
            {
                BuildAssistantIssueSeverity.Error => MessageType.Error,
                BuildAssistantIssueSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };
        }

        private void ApplyWindowTitle()
        {
            string windowIconPath = ResolveBrandingAssetPath(WindowIconFileName, WindowIconFallbackPaths);
            Texture2D windowIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(windowIconPath);
            titleContent = windowIcon != null
                ? new GUIContent("Build Assistant", windowIcon)
                : new GUIContent("Build Assistant");
        }

        private void LoadWindowThumb()
        {
            windowThumbSprite = null;
            windowThumbTexture = null;
            windowThumbSourceRect = Rect.zero;

            string windowThumbPath = ResolveBrandingAssetPath(WindowThumbFileName, WindowThumbFallbackPaths);
            if (!string.IsNullOrWhiteSpace(windowThumbPath))
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(windowThumbPath);
                for (int i = 0; i < subAssets.Length; i++)
                {
                    if (subAssets[i] is Sprite sprite)
                    {
                        windowThumbSprite = sprite;
                        windowThumbTexture = sprite.texture;
                        windowThumbSourceRect = ResolveSpriteSourceRect(sprite);
                        return;
                    }
                }

                windowThumbTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(windowThumbPath);
                if (windowThumbTexture != null && windowThumbTexture.width > 0)
                {
                    windowThumbSourceRect = new Rect(0f, 0f, windowThumbTexture.width, windowThumbTexture.height);
                    return;
                }
            }

            windowThumbTexture = BuildGeneratedThumbTexture();
            if (windowThumbTexture != null)
            {
                windowThumbSourceRect = new Rect(0f, 0f, windowThumbTexture.width, windowThumbTexture.height);
            }
        }

        private string ResolveBrandingAssetPath(string fileName, IReadOnlyList<string> fallbackPaths)
        {
            BuildAssistantSettings currentSettings = settings ?? BuildAssistantSettingsService.GetOrCreateSettings();
            if (currentSettings != null)
            {
                string settingsAssetPath = AssetDatabase.GetAssetPath(currentSettings);
                string settingsDirectory = BuildAssistantPathUtility.NormalizeAssetPath(Path.GetDirectoryName(settingsAssetPath));
                if (!string.IsNullOrWhiteSpace(settingsDirectory))
                {
                    for (int i = 0; i < WindowLogoFolderNames.Length; i++)
                    {
                        string candidatePath = BuildAssistantPathUtility.NormalizeAssetPath(
                            Path.Combine(settingsDirectory, WindowLogoFolderNames[i], fileName));
                        Object asset = AssetDatabase.LoadMainAssetAtPath(candidatePath);
                        if (asset != null)
                        {
                            return candidatePath;
                        }
                    }
                }
            }

            if (fallbackPaths != null)
            {
                for (int i = 0; i < fallbackPaths.Count; i++)
                {
                    string fallbackPath = fallbackPaths[i];
                    if (string.IsNullOrWhiteSpace(fallbackPath))
                    {
                        continue;
                    }

                    Object fallbackAsset = AssetDatabase.LoadMainAssetAtPath(fallbackPath);
                    if (fallbackAsset != null)
                    {
                        return fallbackPath;
                    }
                }
            }

            string assetFoundByName = TryFindAssetPathByName(fileName);
            if (!string.IsNullOrWhiteSpace(assetFoundByName))
            {
                return assetFoundByName;
            }

            return string.Empty;
        }

        private static string TryFindAssetPathByName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string[] guids = AssetDatabase.FindAssets($"{fileNameWithoutExtension} t:Texture2D");
            if (guids == null || guids.Length == 0)
            {
                return string.Empty;
            }

            string preferredPath = string.Empty;
            string fallbackPath = string.Empty;

            for (int i = 0; i < guids.Length; i++)
            {
                string candidatePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(candidatePath))
                {
                    continue;
                }

                string candidateFileName = Path.GetFileName(candidatePath);
                if (!string.Equals(candidateFileName, fileName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (candidatePath.Contains("/Art/UI/Logos/"))
                {
                    preferredPath = candidatePath;
                    break;
                }

                if (string.IsNullOrWhiteSpace(fallbackPath))
                {
                    fallbackPath = candidatePath;
                }
            }

            return !string.IsNullOrWhiteSpace(preferredPath) ? preferredPath : fallbackPath;
        }

        private static Texture2D BuildGeneratedThumbTexture()
        {
            const int width = 960;
            const int height = 220;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color top = new Color32(0x38, 0x1A, 0x66, 0xFF);
            Color bottom = new Color32(0x20, 0x0C, 0x3B, 0xFF);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float t = height <= 1 ? 0f : (float)y / (height - 1f);
                Color rowColor = Color.Lerp(bottom, top, t);
                int rowStart = y * width;
                for (int x = 0; x < width; x++)
                {
                    pixels[rowStart + x] = rowColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Rect ResolveSpriteSourceRect(Sprite sprite)
        {
            try
            {
                Rect textureRect = sprite.textureRect;
                if (textureRect.width > 0f && textureRect.height > 0f)
                {
                    return textureRect;
                }
            }
            catch
            {
                // Ignore and fallback to rect below.
            }

            return sprite.rect;
        }

        #endregion
    }
}
