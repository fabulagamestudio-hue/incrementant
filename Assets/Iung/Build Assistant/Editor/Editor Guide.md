# Editor Guide

## Purpose
Contains Unity Editor-only integration for Build Assistant, including menu entries, services, build interception, asset loading, and the guided editor window.

## When to use
- Use this folder for code that depends on `UnityEditor`, `UnityEditor.Build`, `AssetDatabase`, `EditorWindow`, `EditorGUILayout`, or editor-only build APIs.
- Use editor services to bridge Runtime models and Unity Editor systems.

## Main folders
- `Menus`: Unity menu items that open or select Build Assistant tooling.
- `Services`: workflow logic and integrations with Player Settings, builds, history, validation, assets, and session persistence.
- `Windows`: the Build Assistant editor window UI.

## Rules and conventions
- Keep editor GUI responsibilities separated from asset loading logic.
- Keep editor window drawing code in window classes and move reusable workflow rules into services.
- Keep direct `AssetDatabase` usage inside editor services unless a window needs a strictly visual asset reference already loaded by a service.
- Editor classes may read and write `BuildAssistantSettings`, but should call service methods when persistence or normalization is required.
- Build interception must not block batch mode or Build Assistant-triggered builds.
- User-facing editor dialogs should be explicit about whether a build is being cancelled, continued, or redirected to Build Assistant.

## Prefer
- Keep editor entry points small and delegate behavior to services or windows.
- Use `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets`, and package path utilities through existing services when saving package state.
- Refresh derived session data after changing build identity, versioning, output folder, or folder name composition.

## Avoid
- Avoid direct runtime scene dependencies from editor services.
- Avoid storing transient editor UI state in ScriptableObject settings unless it must persist across editor sessions.
- Avoid adding new editor windows when the existing guided flow can be extended safely.

## Notes for AI assistants
- Before editing any file under this folder, read this guide and the more specific guide in the target subfolder.
- Preserve the current separation between menu entry points, services, and windows.
