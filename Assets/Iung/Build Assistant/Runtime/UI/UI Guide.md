# UI Guide

## Purpose
Contains optional runtime UI components for displaying Build Assistant version information inside a built game.

## Main files
- `BuildAssistantTmpVersionLabel.cs`: TextMeshPro component that formats and applies the latest build version label, profile, numeric parts, timestamp, and suffix tokens. `BuildVersionText(BuildAssistantChangelogEntry)` renders a changelog entry with the same template.

## Rules and conventions
- Runtime UI components currently use the namespace `Iung.BuildAssistant` for public component compatibility.
- Use TextMeshPro (`TMP_Text`) for displayed text.
- Keep the component safe when settings are missing by falling back to successful build history or `Application.version`.
- Keep template validation user-readable and avoid throwing for invalid templates.
- Preserve supported token names unless a compatibility note is added.
- Avoid editor-only APIs in runtime UI components; editor-specific inspector support belongs under `Editor/Services` or another Editor folder.

## Supported template tokens
- `{FullVersion}`: resolved build version label.
- `{Profile}`: resolved build profile label.
- `{Major}`, `{Minor}`, `{Patch}`: numeric version parts when available.
- `{Date}`, `{Time}`: resolved build timestamp parts.
- `{Suffix}`: manual suffix when available.
- `{0}` and `{1}`: legacy aliases for full version and profile.

## Prefer
- Assign `targetText` and `settings` explicitly in the Inspector when possible.
- Keep fallback behavior deterministic when no Build Assistant settings asset is assigned.
- Add new tokens through a single parsing and formatting path.

## Avoid
- Avoid using Unity UI `Text` for new runtime labels.
- Avoid requiring editor services at runtime.
- Avoid making token formatting depend on scene state.

## Notes for AI assistants
- If changing token behavior, update this guide and the user-facing documentation in `Documentation~` when appropriate.
- Preserve the existing fallback chain unless the user explicitly asks for different behavior.
