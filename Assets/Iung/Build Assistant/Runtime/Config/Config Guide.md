# Config Guide

## Purpose
Contains Build Assistant persistent configuration stored as Unity ScriptableObject assets.

## Main files
- `BuildAssistantSettings.cs`: stores defaults, build history, last session state, folder name composition, runtime version metadata, and the runtime changelog (`RuntimeChangelog`, `RecordChangelogEntry`, `ReplaceChangelog`) and the changelog languages (`ChangelogLanguage`, `ChangelogTranslationLanguages`). It also keeps one session per Unity Build Profile (`ProfileSessions`, `FindProfileSession`, `UpdateProfileSession`) and filters the runtime changelog by the profile of the shipped build (`RuntimeBuildProfileId`, `GetRuntimeChangelogForBuild`).

## Rules and conventions
- Configuration classes use the namespace `Iung.BuildAssistant.Config`.
- Keep settings as the persistence boundary for editor sessions and runtime version display.
- Use `EnsureConsistency()` to normalize nested state after loading, validation, and session updates.
- Keep Inspector-facing fields private with `[SerializeField]` and clear `[Tooltip]` text.
- Public access should prefer read-only properties or focused update methods.
- Avoid exposing mutable lists unless existing API compatibility requires it.

## Prefer
- Add focused update methods when settings changes need normalization or synchronization.
- Keep default values safe for a new project importing the package.
- Store only data that must persist across editor sessions or be available at runtime.

## Avoid
- Avoid storing temporary editor window drawing state here.
- Avoid direct AssetDatabase or PlayerSettings calls in config classes.
- Avoid bypassing `EnsureConsistency()` after changing nested settings data.

## Notes for AI assistants
- If adding fields to settings, check whether `BuildAssistantSettingsService`, session persistence, validation, and runtime label behavior also need updates.
