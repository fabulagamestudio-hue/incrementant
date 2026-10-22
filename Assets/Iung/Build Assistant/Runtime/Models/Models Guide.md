# Models Guide

## Purpose
Contains serializable Build Assistant data structures shared by editor services, editor windows, settings, reports, history, and runtime UI.

## Main files
- `BuildAssistantEnums.cs`: workflow, platform, profile, versioning, and validation enums.
- `BuildAssistantFolderNameComposition.cs`: folder name token rules and normalization helpers.
- `BuildAssistantChangelogEntry.cs`: release notes of one version shipped with the build (runtime changelog); `GetNotes(languageCode)` picks the translation or the main notes.
- `BuildAssistantChangelogNotes.cs`: release notes in one extra language (session, history and changelog translations).
- `BuildAssistantHistoryEntry.cs`: recorded build history item data.
- `BuildAssistantProfileSession.cs`: session saved for one Unity Build Profile (id, name, session).
- `BuildAssistantReviewReport.cs`: validation report container.
- `BuildAssistantScriptChangeReport.cs`: script change comparison result.
- `BuildAssistantScriptSnapshot.cs`: stored script snapshot data.
- `BuildAssistantSessionState.cs`: current build workflow state.
- `BuildAssistantValidationIssue.cs`: individual validation issue data.

## Rules and conventions
- Model classes use the namespace `Iung.BuildAssistant.Models`.
- Keep models serializable when they are stored in settings, reports, history, or snapshots.
- Preserve enum numeric values when serialized data or UI step order depends on them.
- Keep `BuildAssistantStep` aligned with the editor window step order.
- Keep public fields stable in serializable state classes unless a migration is intentional.
- Computed properties are acceptable for counts, derived booleans, or read-only summaries.
- Utility methods should be focused on model normalization or model-safe transformations.

## Prefer
- Use explicit names that describe build workflow concepts.
- Keep defaults valid for a new session.
- Normalize collections that represent ordered UI or folder composition rules.
- Keep report models free of editor UI code.

## Avoid
- Avoid adding UnityEditor dependencies to models.
- Avoid adding scene object references to shared workflow models unless they are already part of Unity runtime configuration.
- Avoid changing serialized enum order casually.

## Notes for AI assistants
- If changing a model used by `BuildAssistantSessionState`, check session creation, persistence, preview refresh, validation, and window drawing.
- If changing folder name tokens, check versioning, preview generation, validation, and settings normalization.
