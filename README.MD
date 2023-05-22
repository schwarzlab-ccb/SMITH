# SMITH: Stochastic Model of Intra-Tumor Heterogeneity

SMITH is a tool for fast stochastic simulation of evolution of subclones within a solid tumor. 

We use a confined, well-mixed, branching model of cell populations.

The tool runs large-scale simulations (typically ~billion, but larger orders are also possible with the limitation of 2^31 per clone) and allows for fast evaluation across multiple executions.

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
./plot.sh
```

The results will be written to the folder `./out`

### Options

Use `dotnet run -- [options]` to specify any of the following:

```
  -O, --output    (Default: ./out) The path to the output files.
  -C, --config    (Default: ./sim_params.json) A json file with configuration of the experiment.
  -N              (Default: false) Use newline in logs (useful for batch execution)
```

## Parameters

The following parameter values can be set in the configuration file:

### Simulation options    

For the fitness types a numerical value can be also used, e.g. `"FitnessAcc": 1` is equivalent to `"FitnessAcc": "Add"`.

 * `FitnessAcc: ["Mul", "Add", "Lim"]` The fitness accumulation across all mutations. Either multiplicative (0), additive (1), or asymptotically limited with the max value of 10 (2).
 * `FitnessDist: ["Constant", "Normal", "Exponential", "Uniform"]` The fitness of a mutation is sampled from a distribution. Either constant (0), normal (1), exponential (2), or uniform (3).
 * `FitnessEffect: ["Birth", "Death", "Both"]` The effect of mutation on the fitness of the clone. Either birth (0), death (1), or both (2).
 * `Seed: int` The random seed for the simulation.

### Model
* `Turnover: [0.0-1.0]`  The fraction of cells dividing per step (should be considerably smaller than 1).
* `MutationProb: [0.0-1.0]` The probability of a mutation per cell division.
* `FitnessMean: unsigned double` The mean fitness increase per mutation.
* `ConfGlobal: unsigned double` The global confinement of the population - the higher the confinement the stronger the competition between clones.
* `ConfLocal: unsigned double` The local confinement of the population - the higher the confinement the stronger the restriction of the population of each clone.

### Initialization
* `StartMut: uint` The number of mutation of the cells in the first clone at the start.
* `StartPop: uint` The number of cells in the the first clone at the start.

### Control variables
* `MinPop: uint` The simulation resets if the population dies out before reaching this number.
* `MaxPop: uint` The simulation stops at (or after) this population.
* `MaxSteps: int` The simulation stops at this step. `-1` means no limit.
* `MaxClones: int` The simulation stops at this number of clones. `-1` means no limit.
* `MaxTries: int` The simulation stops if it fails to finish after this number of tries. `-1` means no limit.
* `Reps: uint` How many times the simulation runs.

### Output
* `CutOff: [0.0-1.0]` Only the clones that have at least this fraction of the alive population (e.g. `.01` means at least one percent of alive cells) are included in the output. 
* `CloneSample: int` The number of clones to sample from the population, the clones are sorted by size in descending order, then first `CloneSample` clones are selected. This is ingored if the value is negative.
* `CalcFish: bool` Will include data for Fish Plots in the output. These are not calculated by default as it is storage-intensive.
* `FishFrac: [0.0-1.0]` Similar to `CutOff`, but used for Fish Plots. Unline with `CutOff` a population is included in the output if the fraction has been attained at any step throughout the simulation.


## Test

A test set with minimum simulation is provided. The test is conducted by output comparison. Run:

```
./test.sh
```

## Plot

To plot you can use one of two simple scripts to use depending on whether the simulation was repeated (Reps > 1):  
  
* `./plot.sh <out>` - this will plot the results from a non-repeated experiment. An optional out parameter can be used as an input folder. If not specified, the default output folder `./out` is used.
* `./multiplot.sh out` - this will plot the results from a repeated experiment. A path to an experiment must be specified. Note that in case of repeated experiment, SMITH creates a folder with a timestamp, so for example the default output path for an experiment started on 2023-04-05 16:17:18 will be `./out/21_04_05_16_17_18` and the correct command would be `./multiplot.sh ./out/21_04_05_16_17_18`.

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

#### `clone_tree.dot`

An evolutionary tree with mutation distances and population sizes between the individual subclones. The graph is written in the [DOT format](https://graphviz.org/doc/info/lang.html).

#### `clone_tree.new`

Same as `clone_tree.dot`, but in the [Newick format](https://en.wikipedia.org/wiki/Newick_format)

#### `sim_params.json` 

Stores configuration parameters used for this simulation, including the random seed. If this file is provided on input, the exact same simulation will be executed.

#### `clones.tsv`  

Information about the individual subclones at the end of the simulation.

### Plots

#### `fish.png`, `fish_abs.png`  
Fish plots generated using the [PyFish package](https://bitbucket.org/schwarzlab/pyfish): 

| relative plot (population sizes compared to each other) | absolute plot (population sizes compared to the final sample) |
|---------------------------------------------------------|---------------------------------------------------------------|
| ![](./doc/img/fish.png)                              | ![](./doc/img/fish_abs.png)                                |

#### `clone_tree.png` 
An evolutionary tree describing the individual sampled clones and their total population (labelled nodes), together with their evolutionary distance from the parent (labelled edges).

![](./doc/img/clone_tree.png)

## Figures for Streck et al. 2023
The main and supplementary figures for the 2023 publication (Streck, Kaufmann, and Schwarz) are created using the notebooks in the `article_figures` directory.

Most plotting data is provided in `article_figures/data` and was created from raw data using `article_figures/scripts/create_plotting_data_from_raw.py`.

The exception are the plotting data for the fish plots and individual trajectories which were excluded due to size restrictions (>100MB). The raw fish data for these simulation runs can be generated using the smith config files found in `smith/article_figures/data/fish_plot_configs` and `smith/article_figures/data/trajectories_configs`.

**Noble et al. 2022:**
Raw data was taken from [the Noble et al. 2022 GitHub repository](https://github.com/robjohnnoble/ModesOfEvolution).
To create the `real_data.csv` run the script `smith/article_figures/scripts/combine_real_data.R`. Note that you have to see the variable `NOBLE_REPO_DIR` at the top of the script

## Citation
Please cite as: *Adam Streck, Tom L Kaufmann, Roland F Schwarz, SMITH: Spatially Constrained Stochastic Model for Simulation of Intra-Tumour Heterogeneity, Bioinformatics, 2023; https://doi.org/10.1093/bioinformatics/btad102*

## Contact
Email questions, feature requests and bug reports to Adam Streck, adam.streck@mdc-berlin.de.

## License
SMITH is available under the MIT License.