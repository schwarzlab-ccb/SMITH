# Changelog

All notable changes to this project are documented in this file.

## [1.1.0] - 2026-04-22

### Added
- `-B` / `--bifrucating` CLI option to generate bifurcating tree outputs.
- New optional output files when bifurcating mode is enabled:
  - `bin_tree.dot`
  - `bin_tree.new`
- Automated test suite via `dotnet test` in `tests/SMITH.Tests.csproj`.
- Regression coverage for runner outputs and tree conversion behavior.

### Changed
- Tree node labels in `clone_tree.dot` and `clone_tree.new` now use `cloneid-population_size` (for example `1-149897`) instead of `cloneid:population_size`.
- Program flow was refactored into `SimulationRunner` and output orchestration helpers while preserving core output artifacts.
- Failure handling now returns explicit exit codes for parse, sanity-check, and output-write failures.
- `plot.sh` now supports both single-run and repeated-run layouts from one entry point, including recursive traversal of child run folders.
- Default simulation parameters in `sim_params.json` were updated (including seed, mutation/driver settings, confinement, cutoff, and max population).
- Project layout migrated to `src/` and `tests/` directories.

### Removed
- Root-level `test.sh` in favor of `tests/test.sh`.
- Standalone `multiplot.sh` workflow (behavior folded into `plot.sh`).

### Notes
- This release targets `.NET 10` (`net10.0`).

