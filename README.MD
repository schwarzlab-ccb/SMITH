# Stochastic Model of Intra-Tumor Heterogeneity

SMITH is a tool for fast stochastic simulation of evolution of subclones within a solid tumor. 

We use a confined, well-mixed, branching model of cell populations.

The tool runs large-scale simulations (typically $10^{9}$, but larger orders are also possible) and allows for fast evaluation across multiple executions.

## Requirements

### Simulation

The simulation code is written in **C# 8**. **.NET 5.0** or newer is requred. We recommend installation using *Conda*:
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
bash ./plots.sh
```

The results will be written to the folder `./out`

#### Options

Use `dotnet run -- [options]` to specify any of the following:

```
  -O, --output    (Default: ./out) The path to the output files.
  -C, --config    (Default: ) A json file with configuration of the experiment.
  -N              (Default: false) Use newline in logs (useful for batch execution)
```

## Output


#### `ccf.csv`       

Cancer cell fraction data in the format [subclone, population size, fraction].

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

***

#### `CCF.png` 

#### `fish.png`  

#### `fish_abs.png`   

#### `metrics_relationships.png`

#### `parent_tree.png`          

## Configuration

The parameters for the simulation can be provided using the `-C config.json` run option. The following are the default parameters:

```
{
  "Seed": -1,
  "StartMut": 1,
  "StartPop": 1,
  "Reps": 10,
  "MaxPop": 1048576000,
  "MaxSteps": 1000000,
  "CutOff": 0.001,
  "MinPop": 1000,
  "Turnover": 0.01,
  "Confinement": 0.1,
  "MutationProb": 0.0001,
  "FitnessMean": 0.125,
  "FitnessAcc": 0,
  "FitnessDist": 0,
  "FitnessEffect": 0,
  "Checkpoints": true
}
```


## Contact
Email questions, feature requests and bug reports to Adam Streck, adam.streck@mdc-berlin.de.

## License
SMITH is available under the MIT License.