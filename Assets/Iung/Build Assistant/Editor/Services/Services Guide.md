# Services Guide

## Purpose
Contains isolated editor services used by Build Assistant windows and menus.

## Main files
- `BuildAssistantActivePlatformService.cs`: resolves and labels the active Unity build target.
- `BuildAssistantBuildProfileService.cs`: identifies the active Unity Build Profile (asset GUID + name; `platform_<target>` when none is active) and stamps it on the session (`ApplyActiveProfile`, `IsSessionForActiveProfile`).
- `BuildAssistantBuildService.cs`: executes builds using the current session state.
- `BuildAssistantEditorAssetLoader.cs`: loads editor visuals and package assets.
- `BuildAssistantChangelogService.cs`: records the session notes into the runtime changelog before a build, rolls them back when the build fails, and imports the history. Only translations of languages listed in the settings and with notes go to the build (`GetConfiguredTranslations`).
- `BuildAssistantHistoryService.cs`: records and trims build history.
- `BuildAssistantPathUtility.cs`: normalizes and validates project/package paths.
- `BuildAssistantPaths.cs`: centralizes package asset paths.
- `BuildAssistantPlayerSettingsService.cs`: synchronizes session values with Unity Player Settings.
- `BuildAssistantScriptChangeService.cs`: snapshots and compares script changes.
- `BuildAssistantSessionService.cs`: creates, restores, persists, and previews session state.
- `BuildAssistantSettingsService.cs`: creates, locates, selects, and saves package settings.
- `BuildAssistantThumbnailDrawer.cs`: draws editor thumbnail previews.
- `BuildAssistantTmpVersionLabelEditor.cs`: custom inspector support for the TMP runtime label.
- `BuildAssistantValidationService.cs`: produces build review issues.
- `BuildAssistantVersioningService.cs`: creates labels, build names, and platform-safe version values.

## Rules and conventions
- Services should expose focused public methods and keep implementation details private.
- Services should be deterministic where possible: input session/settings data should clearly produce output labels, reports, paths, or build options.
- Services may use Unity Editor APIs, but Runtime folders must not call services.
- Keep path handling centralized through `BuildAssistantPathUtility` and `BuildAssistantPaths`.
- Keep settings persistence centralized through `BuildAssistantSettingsService`.
- Keep session creation, preview refresh, and session persistence centralized through `BuildAssistantSessionService`.
- Keep validation centralized through `BuildAssistantValidationService` so the Review step has one source of truth.

## Versioning Rules
- Keep `BuildVersionLabel` as the human-readable build label used by folder names, history and runtime UI. It can preserve the selected versioning mode and manual suffix.
- Use `BuildPlayerSettingsVersion` whenever writing to `PlayerSettings.bundleVersion`. This value must stay Apple-safe using exactly three numeric segments separated by dots.
- `Numeric` mode maps to `Major.Minor.Patch` without the visual `v` prefix or manual suffix.
- `DateTime` mode remains available, but its Player Settings value uses `yyyyMMdd.HHmm.0` instead of underscores or text.
- Manual suffixes are allowed for labels and output folders, but must never be written into `PlayerSettings.bundleVersion`.

## Build profile rules
- Each Unity Build Profile has its own configuration: `BuildAssistantSettings.ProfileSessions` (session), a script snapshot (`LastScriptSnapshot_<id>.json`), history entries and changelog entries tagged with `BuildProfileId`/`BuildProfileName`.
- A profile never configured starts from the last session (without release notes when those belong to another profile). A profile without its own script snapshot compares against the legacy shared `LastScriptSnapshot.json`.
- A changelog version is identified by version label + profile id, so two profiles can both ship `v1.0.0`. The runtime changelog UI reads `GetRuntimeChangelogForBuild()` (entries of the shipped profile plus untagged legacy ones).
- Validation blocks the build when the session was prepared for another profile than the active one.

## Build and history rules
- Only Build Assistant-triggered builds should bypass the default-build interceptor.
- Batch mode must not show blocking dialogs.
- Successful and failed build attempts should be recorded with enough context for the history list.
- History trimming must respect `BuildAssistantSettings.MaxHistoryEntries`.
- Runtime version state should be prepared before build execution so runtime labels can display the intended version.

## Prefer
- Add new service methods to the service that already owns the data or workflow.
- Return report objects or validation issues instead of mixing user-facing UI drawing with service logic.
- Keep exception handling user-readable in editor logs and dialogs.

## Avoid
- Avoid making services depend on `BuildAssistantWindow` except for narrow editor flow actions that intentionally open or focus the window.
- Avoid duplicating path constants outside `BuildAssistantPaths`.
- Avoid writing directly to `PlayerSettings.bundleVersion` with a display label.

## Notes for AI assistants
- Preserve the distinction between display labels and Player Settings versions.
- Do not introduce service-level UI drawing unless the service is specifically a drawing helper.
- When changing build execution or validation, check whether history, review, player settings, and runtime version state also need updates.
