# Changelog

## Unreleased

### Changed

- Unified the version line with `CodeLogic.Libs` on **4.8.x**. The framework and every
  official library now share one `major.minor`, so a given `4.8.<patch>` means the same
  generation across all packages.
- Moved `major.minor` into a hand-controlled repo-root `version.txt`. The patch component
  is the CI run number, composed at pack time — no version is written back to the
  repository during a release.
- `AssemblyVersion` and `FileVersion` are now derived as `Major.Minor.0.0` (`4.8.0.0`)
  instead of being hardcoded. Assembly binding stays fixed across the whole `4.8` line,
  so every patch loads interchangeably without a binding redirect.

### Removed

- Removed commit-message-driven version bumping from the release workflow. `feat:` and
  `!`/`BREAKING CHANGE` prefixes no longer move `major` or `minor`; the version line moves
  only when `version.txt` is edited.

### Added

- Added a `prerelease` branch trigger to the release workflow, matching `CodeLogic.Libs`.
  Pushing to `prerelease` publishes `4.8.<run>-preview+<sha>`; pushing to `release`
  publishes `4.8.<run>+<sha>`.
- Added [Reference/versioning.md](docs/Reference/versioning.md) documenting the scheme,
  the run-number patch, assembly binding identity, and how to move to the next minor.

### Notes

- The previous published line was `4.1.x` with `AssemblyVersion` pinned at `3.3.0.0`.
  Assembly identity now moves to `4.8.0.0`, which is a binding-breaking change for
  anything compiled against the old value; recompilation is required.
