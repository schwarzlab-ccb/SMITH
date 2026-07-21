# Changelog

All notable changes to this project are documented in this file.

## [1.3.0] - 2026-07-21

### Changed
- `MutationProb` now directly specifies the driver-mutation probability per new cell. Bundled
  configs were updated to preserve their previous effective driver-event rates (previously
  `MutationProb × DriverProb`); the `_bulk` configs also lower `CutOff` to `0.001`.
- `clone_tree.new` now uses simulation steps as branch lengths. Each branching node records the
  parent clone's population at that step and branches into the parent's self-continuation plus the
  newly appearing subclone lineage(s). Multifurcations are limited to same-step appearances.
- The Nextflow workflow now honors `--only` and `--max_forks`, exposes `--conda_env` and
  `--slurm_options`, uses the repository conda environment and Release executable, and detects
  completed fish and non-fish runs reliably.

### Fixed
- `MaxClones` is now enforced while a generation is created, mutation counts retain 64-bit range,
  and binomial sampling above one billion trials uses independent chunks without inflating variance.
- Empty clone selections and zero-alive outputs now produce finite zero-valued metrics, and
  checkpoint generation rejects a zero `MinPop` while avoiding overflow at large population limits.

### Removed
- Passenger mutation sampling, the `DriverProb` configuration parameter, the passenger clone state,
  and the `Passengers` column from `clones.csv`.

### Migration
- Config files that still carry a `DriverProb` field load without error but the field is ignored,
  so the effective driver rate becomes `MutationProb` alone. Update old configs by setting
  `MutationProb := MutationProb × DriverProb` and deleting `DriverProb`.
- The `clone_tree.new` Newick format changed: branch lengths are now simulation steps (not driver
  distances) and the file ends with a root-stem length (`…)label:rootDistance;`). Downstream
  parsers that assumed the previous format must be updated.

## [1.2.1] - 2026-06-12

### Fixed
- Branch lengths in `clone_tree.new` are now preserved when multifurcations are resolved into a bifurcating tree. Previously, every child of a node with three or more children was written with a hardcoded distance of `1`, so those branch lengths no longer matched the `Distance` column in `clones.csv`. Each child now keeps its original distance, while the inserted chain (self) nodes remain at distance `0`.

### Added
- `plot.sh` now renders `clone_tree.new` as a phylogenetic tree image (`phylogenetic_tree.png`) using Biopython's `Bio.Phylo`, labelling only the tips with their `cloneid-population_size`.

## [1.2.0] - 2026-06-11

### Changed
- Tree edge distances in `TreeBuilder` now accumulate lineage distance from each traversed `Clone.Distance` value instead of counting ancestor hops (`+1`).
- This affects both collapsed parent trees (`BuildCTree`) and LCA trees (`BuildLCAT`) so branch lengths reflect stored clone distances.
- The `clone_tree.new` Newick output is now always bifurcating; multifurcations are resolved in order of clone appearance.
- `clones.csv` now includes a `FirstGen` column (third column) recording the generation in which each clone first appeared.
- Regression fixture targets in `tests/targets` were refreshed to match current simulation outputs after the tree-distance update.

### Added
- Regression tests in `tests/Computation/TreeBuilderTests.cs` for distance accumulation in both parent-tree and LCA-tree construction.

### Removed
- `-B` / `--bifrucating` CLI option and the separate `bin_tree.dot` / `bin_tree.new` output files; the Newick output is now always bifurcating.

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

