# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

SMITH (Stochastic Model of Intra-Tumor Heterogeneity) is a fast stochastic simulator of subclonal
evolution in a solid tumor, using a confined, well-mixed, branching model of cell populations. The
simulator is C# (.NET 10); the plotting/analysis layer is Python driven by shell scripts.

## Environment

All tooling (both `dotnet` and the Python plotting deps) lives in the `smith` conda environment:

```
conda env create --file SMITH.yml   # first time
conda activate smith
```

`dotnet` is only on `PATH` inside that env — activate it before any build/test/run command.

## Commands

```
dotnet build                                   # build the simulator (tests/** is excluded from SMITH.csproj)
dotnet run                                      # run with ./sim_params.json, output to ./out
dotnet run -- -C ./doc/doc_config.json -O ./out # run with an explicit config and output dir (-N adds newlines for batch logs)
dotnet test                                     # run the xUnit suite (tests/SMITH.Tests.csproj)
dotnet test --filter FullyQualifiedName~TreeBuilderTests   # run a single test class
./plot.sh <out>                                 # render fish/clone/phylogenetic plots for a run (defaults to ./out)
```

`dotnet build` and `dotnet test` are what CI (`.github/workflows`) runs on push.

## How a simulation is wired together

`Program.cs` → `SimulationRunner.RunAll()` → `RunSingleRepeat()` is the whole control flow:

- **Config in, config out.** A run is fully determined by `sim_params.json` (including `Seed`). The
  same file is written back into the output folder as `sim_params.json`, so any run is exactly
  reproducible by feeding that file back in with `-C`. `SimParams.SanityCheck()` gates startup.
- **The step loop.** `Simulator.Step()` advances one generation: for each live clone it samples
  deaths, births, and new mutations via `Extreme.Numerics` binomial/geometric draws off the shared
  `Random`. **The RNG stream is order- and count-sensitive** — changing how many samples are drawn,
  or their order, changes every downstream result and every regression fixture. A driver mutation
  spawns a child `Clone`; passengers are counted but do not fork clones.
- **Retries.** If the population dies out before `MinPop`, the repeat restarts with the next seed
  draw (`TryNo` in the output). `Reps` controls how many independent simulations run.
- **Checkpoints & output.** `State.GetCompState` decides Running/Finished; at `Finished` (and at
  log2-size checkpoints) `SimulationAnalysis.AnalyzeCheckpoint` filters clones by `CutOff`/
  `CloneSample`, builds the tree, and `SimulationResultOutput.WriteFinishedOutputs` writes the files.

## Clones, trees, and distances (the part that spans multiple files)

- `Clone.Distance` is the number of **new mutation events** on the branch from its parent (drivers +
  passengers gained). `Simulator` sets it as `passengerMutantCount + 1` (the `+1` is the driver).
- Passenger counts come from `CalcPassengers`, which draws from `Extreme`'s `GeometricDistribution`.
  That distribution counts *trials until the first success* (support ≥ 1, mean `1/p`), so it includes
  the driver trial itself — the sample is decremented by 1 to yield passenger mutations only. Getting
  this off-by-one wrong doubles every event distance in the output tree.
- `TreeBuilder` turns the flat clone list into trees. `BuildLCAT` (used for output) keeps sampled
  clones plus the lowest-common-ancestor internal nodes and sums `Distance` along collapsed edges.
  `ConvertToBifrucatingNodes` then forces a binary tree, resolving multifurcations into a chain of
  zero-length self-nodes ordered by clone appearance (`FirstGen`); those filler nodes carry the
  parent's label. The Newick output is therefore always bifurcating.

## Output files

Written to the output folder (see README.MD for full column docs): `clones.csv` (per-clone state),
`clone_tree.dot`/`clone_tree.new` (evolutionary tree in DOT/Newick, node labels `cloneid-popsize`,
branch length = `Distance`), `parent_tree.csv` + `populations.csv` (PyFish fish-plot inputs, only
when `CalcFish`), and `summary.csv` (per-checkpoint statistics; its `Time` column is wall-clock and
therefore non-deterministic).

## Tests

`SimulationRunnerRegressionTests` is a golden-file test: it runs the fixture config in
`tests/targets/test_params.json` and does an **exact text diff** of the output against the checked-in
files in `tests/targets/`. Any intentional change to simulation numerics, RNG usage, or output
formatting will break it — regenerate the fixtures by copying a fresh run's output over
`tests/targets/*` (the test writes its run to `tests/out/`). Note `summary.csv` is compared by
*shape* (column counts) only, because of its non-deterministic `Time` column.
