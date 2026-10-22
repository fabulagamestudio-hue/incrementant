# Windows Guide

## Purpose
Contains the Build Assistant editor window, which presents the guided build workflow and coordinates user interaction.

## Main files
- `BuildAssistantWindow.cs`: IMGUI-based editor window for context, identity, runtime settings, versioning, review, changelog, script changes, build progress, and navigation.

## Rules and conventions
- Editor windows should avoid direct AssetDatabase lookup logic.
- The window should own visual state, scroll positions, tab indices, reorderable lists, and draw methods.
- The window should delegate business rules, validation, build execution, history, asset loading, and session persistence to services.
- Navigation should protect unsaved changelog edits before switching steps, changelog tabs or changelog languages.
- The changelog panel shows a language toolbar only when the settings list extra languages; index 0 is the main language (session fields), the others edit `session.ChangelogTranslations`.
- `SyncSessionFromActiveProfile` runs every `OnGUI`: when the active Unity Build Profile differs from the session's, it saves the current session and loads the one of the new profile (composer, preview and review rebuilt). Recent history and the size estimate only count the session's profile.
- Whenever session data changes, refresh preview data and review reports when those derived values may be affected.
- Keep IMGUI drawing grouped by workflow step and helper purpose.

## Prefer
- Add small draw helper methods instead of expanding a single large `OnGUI` block.
- Keep user-facing labels and warnings near the UI section that displays them.
- Use existing services when the window needs paths, settings, validation, build execution, or versioning.
- Preserve the step order defined by `BuildAssistantStep` and the window's step labels.

## Avoid
- Avoid direct file system or AssetDatabase searches from draw methods.
- Avoid doing expensive refresh work every `OnGUI` call unless the data is intentionally live.
- Avoid storing business rules in tab rendering code when a service can own them.

## Notes for AI assistants
- Before changing this folder, read the root BuildAssist guide, `Editor/Editor Guide.md`, and this guide.
- If a window change requires new validation, implement or update the validation service rather than only drawing a warning.
