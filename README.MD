# SMITH 2022: Stochastic Model of Intra-Tumor Heterogeneity (Article Version)

SMITH is a tool for fast stochastic simulation of evolution of subclones within a solid tumor. 

We use a confined, well-mixed, branching model of cell populations.

The tool runs large-scale simulations (typically ~billion, but larger orders are also possible) and allows for fast evaluation across multiple executions.

## Requirements

### TL;DR; 

The program can be run on a platform of your choice in the provided *Conda* environment. Inside of the repo run 
```
conda env create --file SMITH.yml
conda activate smith
```

### Tested platforms

The program has been tested on:
* Windows 10 - PowerShell
* Windows 10 - WSL2 Ubuntu
* Ubuntu 20
* MacOS X 10 

### Simulation

The simulation code is written in **C# 8**. **.NET 5.0** or newer is required. We recommend installation using *Conda*:
```
conda install -c conda-forge dotnet
``` 

### Data analysis

The analysis code is written in **Python 3.8**. The following packages are required (either from *Conda* or *Pip*):
```
conda install -c bioconda pyfish
conda install -c conda-forge biopython matplotlib numpy pandas seaborn pillow
```

## Execution

The default execution is:
```
git clone git@bitbucket.org:schwarzlab/smith.git
cd smith
dotnet run
./plots.sh
```

The results will be written to the folder `./out`

### Options

Use `dotnet run -- [options]` to specify any of the following:

```
  -O, --output    (Default: ./out) The path to the output files.
  -C, --config    (Default: ) A json file with configuration of the experiment.
  -N              (Default: false) Use newline in logs (useful for batch execution)
```

## Parameters

The following parameter values can be set in the configuration file:

### Simulation options
 * `FitnessAcc: [0,1,2]` The fitness accumulation across all mutations. Either multiplicative (0), additive (1), or limited multiplicative (2).
 * `FitnessDist: [0,1,2,3]` The fitness of a mutation is sampled from a distribution. Either constant (0), normal (1), exponential (2), or uniform (3).
 * `FitnessEffect: [0,1,2]` The effect of mutation on the fitness of the clone. Either birth (0), death (1), , or both (2).
 * `Seed: int` The random seed for the simulation.

### Control variables
* `MinPop: uint` The simulation resets if the population dies out before reaching this number.
* `MaxPop: uint` The simulation stops at (or after) this population.
* `MaxSteps: uint` The simulation stops at this step.
* `MaxClones: uint` The simulation stops at this number of clones.
* `Reps: uint` How many times the simulation runs.

### Output
* `CutOff: [0.0-1.0]` Only clones with the following fraction are considered in analysis (ignores microclones).
* `CloneSample: int` The number of clones to sample from the population, the clones are sorted by size in descending order, then first `CloneSample` clones are selected. This is ingored if the value is negative.

### Model
* `Turnover: [0.0-1.0]`  The fraction of cells dividing per step (should be considerably smaller than 1).
* `MutationProb: [0.0-1.0]` The probability of a mutation per cell division.
* `FitnessMean: [0.0-1.0]` The mean fitness increase per mutation.
* `Confinement: [0.0-1.0]` The higher the confinement the stronger the competition between clones.

### Initialization
* `StartMut: uint` The number of mutation of the cells in the first clone at the start.
* `StartPop: uint` The number of cells in the the first clone at the start.

## Test

A test set with minimum simulation is provided. The test is conducted by output comparison. Run:

```
./plots.sh
```

## Output

The following output was generated using the [demo configuration](./doc/doc_config.json). 

To reproduce the results run `dotnet run -- -C ./doc/doc_config.json`.

The text files are primariliy used as source for plots shown below.

#### `parent_tree.csv` 

Describes the parent-child relationship between subclones. Used for plotting of Fish Plots. For details see [PyFish repository](https://bitbucket.org/schwarzlab/pyfish/).

#### `populations.csv`

Lists population sizes at individual timepoints for each subclone.  Used for plotting of Fish Plots.  For details see [PyFish repository](https://bitbucket.org/schwarzlab/pyfish/).

#### `summary.csv`     

Statistical / analytical eveluation of the simulation at individual stops. If checkpoints are used, log2 sizes are considered as stops, starting from the minimum size. Otherwise only one output at the end of the simulation is printed.

#### `parent_graph.dot`

An evolutionary tree with mutation distances and population sizes between the individual subclones. The graph is written in the [DOT language](https://graphviz.org/doc/info/lang.html).

#### `sim_params.json` 

Stores configuration parameters used for this simulation, including the random seed. If this file is provided on input, the exact same simulation will be executed.

#### `subclones.out`  

Information about the individual subclones at the end of the simulation.

### Plots (created by `./plots.sh`)

#### `fish.png`, `fish_abs.png`  
Fish plots generated using the [PyFish package](https://bitbucket.org/schwarzlab/pyfish): 

| relative plot (population sizes compared to each other) | absolute plot (population sizes compared to the final sample) |
|---------------------------------------------------------|---------------------------------------------------------------|
| ![CCF](./doc/img/fish.png)                              | ![CCF](./doc/img/fish_abs.png)                                |

#### `metrics_relationships.png`
Plot the pairwise relationship between every pair of metrics present in `summary.csv` as well as their indiviudal distributions. This is usually used in the initial assesment of the simulation as a quick overview of the values produced.

![CCF](./doc/img/metrics_relationships.png)

#### `parent_graph.png` 
An evolutionary tree describing the individual sampled clones and their total population (labelled nodes), together with their evolutionary distance from the parent (labelled edges).

![CCF](./doc/img/parent_graph.png)

## Figures for Streck et al. 2022
The main and supplementary figures for the 2022 publication (Streck, Kaufmann and Schwarz) are created using the notebooks in the `smith/notebooks` directory.

Most plotting data is provided in `smith/notebooks/data` and was created from raw data using `smith/notebooks/scripts/create_plotting_data_from_raw.py`.

The exception are the plotting data for the fish plots and individual trajectories which were excluded due to size restrictions (>100MB). The raw fish data for these simulation runs can be generated using the smith config files found in `smith/notebooks/data/fish_plot_configs` and `smith/notebooks/data/trajectories_configs`.

**Noble et al. 2022:**
Raw data was taken from [the Noble et al. 2022 GitHub repository](https://github.com/robjohnnoble/ModesOfEvolution).
To create the `real_data.csv` run the script `smith/notebooks/scripts/combine_real_data.R`. Note that you have to see the variable `NOBLE_REPO_DIR` at the top of the script


## Contact
Email questions, feature requests and bug reports to Adam Streck, adam.streck@mdc-berlin.de.

## License
SMITH is available under the MIT License.