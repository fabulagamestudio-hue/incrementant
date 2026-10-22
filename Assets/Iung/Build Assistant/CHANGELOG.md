# Changelog

All notable changes to this package will be documented in this file.

## [Unreleased]

### Added

- Configuration per Unity Build Profile: each profile keeps its own session (output folder, identity, runtime, version, notes, folder name), script snapshot, history and runtime changelog; the window follows the profile active in Unity.
- Changelog notes per language: main language plus extra languages in the settings, one tab per language in the Versioning step, translations stored in the session, history and runtime changelog, and `BuildAssistantChangelogEntry.GetNotes(languageCode)` for runtime UI.
- Review warning when an extra language has no notes for the build.

## [1.0.0] - 2026-05-17

### Added

- Initial release.
- Build Assistant editor tooling.
- Build version management support.
- Build validation workflow support.
- Build settings asset.
- Runtime TextMeshPro version label support.
