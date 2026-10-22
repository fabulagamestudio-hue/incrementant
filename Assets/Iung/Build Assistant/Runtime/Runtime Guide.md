# Runtime Guide

## Purpose
Contains runtime-safe code shared by Build Assistant editor tooling and optional in-game version display.

## When to use
- Use Runtime code for serializable settings, models, enums, reports, and components that may be compiled into a player build.
- Use Editor code instead when a feature needs `UnityEditor`, build pipeline APIs, AssetDatabase, or editor windows.

## Main folders
- `Config`: persistent ScriptableObject settings used by editor tooling and runtime version labels.
- `Models`: serializable workflow state, reports, validation data, and folder name composition helpers.
- `UI`: optional runtime UI components that display build metadata.

## Rules and conventions
- Runtime code must not reference `UnityEditor` APIs.
- Runtime code may reference UnityEngine and TextMeshPro only where needed by runtime components.
- Keep data models serializable and stable because fields may be stored inside settings assets or Unity serialized data.
- Preserve public field names in serializable models unless a migration is planned.
- Use block-scoped namespaces that match the existing folder convention.

## Prefer
- Keep shared workflow data in models instead of editor-only classes when it needs to persist or be displayed at runtime.
- Keep runtime UI optional and safe when settings are missing.
- Add normalization utilities close to the model they normalize.

## Avoid
- Avoid editor-only dialogs, asset database calls, or build pipeline calls in Runtime.
- Avoid scene-specific dependencies in shared models.
- Avoid creating runtime dependencies on editor services.

## Notes for AI assistants
- Before editing Runtime code, read this guide and the nearest folder guide.
- If adding serialized fields, preserve backward compatibility and add clear tooltips when the field is Inspector-facing.
