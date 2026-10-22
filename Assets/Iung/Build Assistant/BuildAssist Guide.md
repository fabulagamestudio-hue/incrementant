# BuildAssist Guide

## Purpose
Build Assistant is a Unity editor package that centralizes build preparation, validation, versioning, folder naming, history tracking, and optional runtime version display.

## When to use
- Use this package when a project needs a guided build workflow instead of relying only on Unity's default Build Settings window.
- Use the editor window to prepare release context, identity, runtime options, version labels, changelog notes, script change reports, and final build execution.
- Use the runtime label component only when the built game needs to show the latest prepared version metadata in TextMeshPro UI.
- Read `BuildAssistantSettings.RuntimeChangelog` when the built game needs to list the release notes of every version. The session notes are recorded right before each build (keyed by version label, newest first, not trimmed by `maxHistoryEntries`) and rolled back if the build fails; `Iung/Build Assistant/Import History Into Runtime Changelog` seeds it from the history.
- Changelog languages: the notes fields are the main language (`changelogLanguage` in the settings, default `en`); each code in `changelogTranslationLanguages` (e.g. `pt-BR`) gets its own language tab in the Versioning step. Runtime UI calls `BuildAssistantChangelogEntry.GetNotes(localeCode)`, which returns the translation (exact code, then same language) or the main notes.

## Main folders
- `Runtime/Config`: contains persistent package settings stored as ScriptableObject data.
- `Runtime/Models`: contains serializable data containers, enums, reports, and utility models shared by runtime and editor assemblies.
- `Runtime/UI`: contains optional runtime-facing UI components.
- `Editor/Menus`: contains Unity menu entry points.
- `Editor/Services`: contains isolated editor workflow, validation, versioning, history, player settings, and asset loading services.
- `Editor/Windows`: contains the Build Assistant editor window and its IMGUI flow.
- `Documentation~`: contains user-facing package documentation for Unity Package Manager.

## Architecture
- Runtime code must remain independent from `UnityEditor` APIs.
- Editor code may depend on Runtime models and configuration, but Runtime code must not depend on Editor services or windows.
- The editor window should orchestrate the workflow and delegate business rules to services.
- Services should be stateless or operate on explicit `BuildAssistantSessionState` and `BuildAssistantSettings` data whenever possible.
- Serializable models should be simple data objects with minimal behavior, except for focused normalization or computed properties.

## Namespace rules
- Runtime configuration classes use `Iung.BuildAssistant.Config`.
- Runtime model classes use `Iung.BuildAssistant.Models`.
- Runtime UI components currently use the root namespace `Iung.BuildAssistant` for public component compatibility.
- Editor menu classes use `Iung.BuildAssistant.Editor.Menus`.
- Editor window classes use `Iung.BuildAssistant.Editor.Windows`.
- Editor services use `Iung.BuildAssistant.Services`.
- Preserve existing namespaces when modifying existing classes.

## Rules and conventions
- Keep all code compatible with Unity 2021.3 or newer unless the package requirement changes.
- Keep TextMeshPro as the runtime text dependency for version labels.
- Do not introduce `UnityEditor` references in Runtime folders.
- Prefer serialized fields with clear tooltips for user-facing Unity Inspector options.
- Keep build labels and Player Settings versions separate: display labels can be human-readable, while `PlayerSettings.bundleVersion` must remain platform-safe.
- Keep build history bounded by the configured maximum entry count.
- Normalize paths before comparing or writing them.
- Keep package defaults safe for a newly imported project.

## Prefer
- Add rules to the nearest folder guide when behavior is local to a folder.
- Add root-level guidance only for package-wide architecture or conventions.
- Reuse existing models and services before adding new workflow state.
- Add validation issues instead of silently blocking a build when the user can fix the problem.
- Keep generated build folder names deterministic and based on session state.

## Avoid
- Avoid direct AssetDatabase lookup logic in windows.
- Avoid placing editor-only behavior in Runtime folders.
- Avoid duplicating version formatting logic across services, models, and UI components.
- Avoid changing public serialized field names without a migration reason.
- Avoid using runtime UI components as editor workflow dependencies.

## Notes for AI assistants
- Read this guide first, then read the closest folder-level guide before changing files.
- If editing code under `Editor/Services`, also read `Editor/Services/Services Guide.md`.
- If editing code under `Editor/Windows`, also read `Editor/Windows/Windows Guide.md`.
- If editing Runtime code, read the matching Runtime folder guide before changing code.
- Preserve package structure and namespaces unless the user explicitly asks for a migration.
