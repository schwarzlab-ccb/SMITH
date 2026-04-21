# %%
def _enable_ipython_autoreload():
    try:
        from IPython import get_ipython
        ip = get_ipython()
        if ip is not None:
            ip.run_line_magic('load_ext', 'autoreload')
            ip.run_line_magic('autoreload', '2')
    except Exception:
        pass

_enable_ipython_autoreload()

import pickle
import shutil
from pathlib import Path
import pandas as pd
import Bio.Phylo

from pyfish.core import *
from plotting import *


# %%

SCRIPT_DIR = Path(__file__).resolve().parent
ARTICLE_FIGURES_DIR = SCRIPT_DIR.parent
REPO_ROOT = ARTICLE_FIGURES_DIR.parent
DATA_DIR = ARTICLE_FIGURES_DIR / 'data'
RESULTS_DIR = REPO_ROOT / 'out' / 'results'

# %%
# Metrics over time
cur_MutationProb = 1e-5
cur_FitnessMean = 0.1

confinement_data_df, confinement_data_fish, confinement_lines = load_final_confinement_revisions_all()
confinement_data_df['Sweep'] = confinement_data_df['ClonalDiversity'] < 1.5
confinement_data_df = confinement_data_df.drop(['fraction_necro', 'fraction_alive_cells'], axis=1)
confinement_data_df.to_pickle(DATA_DIR / 'main_data.pkl')
all_metrics_over_time = dict()

for global_conf, local_conf in zip([0, 0.5, 0, 0.5], [0, 0, 0.0625, 0.25]):
    metrics_over_time = load_final_metrics_over_time_revisions(
        cur_MutationProb, cur_FitnessMean, global_conf, local_conf)
    all_metrics_over_time[(global_conf, local_conf)] = metrics_over_time

with open(DATA_DIR / 'metrics_over_time.pkl', 'wb') as f:
    pickle.dump(all_metrics_over_time, f)


# %%
# Individual trajectories
example_trajectories = {
    (0, 0): [0, 25, 97, 74.],
    (0.5, 0): [67, 33, 54, 56],
    (0, 0.0625): [49, 1, 36, 98],
    (0.5, 0.25): [97, 24, 2, 12.]}

trajectories = dict()
for cur_Confinement_global, cur_Confinement_local in zip(
        [0, 0.5, 0, 0.5], [0, 0, 0.0625, 0.25]):

    cur_samples = example_trajectories[(cur_Confinement_global, cur_Confinement_local)]

    for i, r in enumerate(cur_samples):
        cur_folder = RESULTS_DIR / f'parameter_range_{cur_MutationProb:.6f}_{cur_FitnessMean:.6f}_{cur_Confinement_global:.6f}_{cur_Confinement_local:.6f}_{int(r)+1}'
        full_population = pd.read_csv(cur_folder / 'populations.csv')
        if 'Drivers' not in full_population.columns:
            clones_df = pd.read_csv(cur_folder / 'clones.csv')[['ID', 'Drivers']].rename(columns={'ID': 'Id'})
            full_population = full_population.merge(clones_df, on='Id', how='left')
        parent_tree_df = pd.read_csv(cur_folder / 'parent_tree.csv')
        fish_data = process_data(full_population, parent_tree_df, absolute=False, smooth=0)
        population_df = fish_data[0].groupby('Id').sum()

        trajectories[(cur_Confinement_global, cur_Confinement_local, i)
                     ] = (full_population, population_df)

        shutil.copy2(
            cur_folder / 'sim_params.json',
            DATA_DIR / 'trajectories_configs' / f'{cur_MutationProb:.6f}_{cur_FitnessMean:.6f}_{cur_Confinement_global:.6f}_{cur_Confinement_local:.6f}_{int(r)+1}_sim_params.json')

with open(DATA_DIR / 'trajectories.pkl', 'wb') as f:
    pickle.dump(trajectories, f)


# %%
# Fitness distribution and accumulation data
selection_confinement_global = [0, 0.0625, 0.125, 0.25, 0.5, 1]
selection_confinement_local = [0, 0.0625, 0.125, 0.25]

try:
    fitness_dist_data = load_final_fitness_revisions_all()

    fitness_dist_data = fitness_dist_data.loc[(fitness_dist_data['MutationProb'] == 1e-5)
                                              & (fitness_dist_data['FitnessMean'] == 0.1)
                                              & (fitness_dist_data['FitnessAcc'] == 'Add')
                                              ]

    selection_confinement_global = [0, 0.0625, 0.125, 0.25, 0.5, 1]
    selection_confinement_local = [0, 0.0625, 0.125, 0.25]

    fitness_acc_data = load_final_fitness_revisions_all()

    fitness_acc_data = fitness_acc_data.loc[(fitness_acc_data['MutationProb'] == 1e-5)
                                            & (fitness_acc_data['FitnessMean'] == 0.1)
                                            & (fitness_acc_data['FitnessDist'] == 'Exponential')
                                            ]
    fitness_acc_data['FitnessAcc'] = fitness_acc_data['FitnessAcc'].apply(
        lambda x: {'ETH': 'Asy'}.get(x, x))
except ValueError as e:
    # Raw fitness summary files may be unavailable in local out/results.
    # In that case, reuse the repository's precomputed plotting inputs.
    if 'No objects to concatenate' not in str(e):
        raise
    fitness_dist_data = pd.read_pickle(DATA_DIR / 'fitness_dist_data.pkl')
    fitness_acc_data = pd.read_pickle(DATA_DIR / 'fitness_acc_data.pkl')

fitness_dist_data.to_pickle(DATA_DIR / 'fitness_dist_data.pkl')
fitness_acc_data.to_pickle(DATA_DIR / 'fitness_acc_data.pkl')


# %%
# Most representative runs per confinement combination

cur_MutationProb = 1e-5
cur_FitnessMean = 0.1

selected_confinement_data_df = confinement_data_df.loc[(confinement_data_df['MutationProb'] == cur_MutationProb) & (
    confinement_data_df['FitnessMean'] == cur_FitnessMean)]
selected_confinement_data_df = selected_confinement_data_df.loc[~selected_confinement_data_df[[
    'ClonalDiversity', 'MeanDriversPerCell']].isna().any(axis=1)]
selected_confinement_data_df['index'] = selected_confinement_data_df.index

group_means = selected_confinement_data_df.groupby(['Confinement_global', 'Confinement_local'])[
    ['MeanDriversPerCell', 'ClonalDiversity']].transform('mean')

sq_diff = ((selected_confinement_data_df[['MeanDriversPerCell', 'ClonalDiversity']] - group_means) ** 2).sum(axis=1)
sq_diff.index = selected_confinement_data_df.set_index(
    ['Confinement_global', 'Confinement_local', 'RepeatId', 'index']).index

most_representative_runs = pd.DataFrame(sq_diff
                                        .sort_values()
                                        .groupby(['Confinement_global', 'Confinement_local'])
                                        .head(1)
                                        .reset_index()
                                        .sort_values(['Confinement_global', 'Confinement_local', 0])
                                        .rename({0: 'score'}, axis=1)
                                        )

most_representative_runs['MutationProb'] = cur_MutationProb
most_representative_runs['FitnessMean'] = cur_FitnessMean
most_representative_runs.to_pickle(DATA_DIR / 'most_representative_runs.pkl')


# %%
# Fish plot data
fish_plot_data = dict()
cur_selection_confinement_global = [0, 0.0625, 0.125, 0.5, 1, 2]
cur_selection_confinement_local = [0, 0.0625, 0.125, 0.25, 0.5]

for cur_Confinement_global in cur_selection_confinement_global:
    for cur_Confinement_local in cur_selection_confinement_local:
        row = most_representative_runs.loc[(most_representative_runs['Confinement_global'] == cur_Confinement_global) & (
            most_representative_runs['Confinement_local'] == cur_Confinement_local)].iloc[0]
        fish_plot_data[(row['Confinement_global'], row['Confinement_local'])] = load_single_fish_data(
            *row[['MutationProb', 'FitnessMean', 'Confinement_global', 'Confinement_local', 'RepeatId']].values)

        fish_config_folder = RESULTS_DIR / f'parameter_range_{row["MutationProb"]:.6f}_{row["FitnessMean"]:.6f}_{row["Confinement_global"]:.6f}_{row["Confinement_local"]:.6f}_{int(row["RepeatId"])+1}'
        shutil.copy2(
            fish_config_folder / 'sim_params.json',
            DATA_DIR / 'fish_plot_configs' / f'{row["MutationProb"]:.6f}_{row["FitnessMean"]:.6f}_{row["Confinement_global"]:.6f}_{row["Confinement_local"]:.6f}_{int(row["RepeatId"])+1}_sim_params.json')

with open(DATA_DIR / 'fish_plot_data.pkl', 'wb') as f:
    pickle.dump(fish_plot_data, f)


# %%
# Tree data
tree_data = dict()
cur_selection_confinement_global = [0, 0.5, 0, 1, 0.5, 2]
cur_selection_confinement_local = [0, 0, 0.0625, 0.25, 0.125, 0.125]
for cur_Confinement_global, cur_Confinement_local in zip(cur_selection_confinement_global, cur_selection_confinement_local):
    row = most_representative_runs.loc[(most_representative_runs['Confinement_global'] == cur_Confinement_global) & (
        most_representative_runs['Confinement_local'] == cur_Confinement_local)].iloc[0]
    cur_folder = RESULTS_DIR / f"parameter_range_{row['MutationProb']:.6f}_{row['FitnessMean']:.6f}_{row['Confinement_global']:.6f}_{row['Confinement_local']:.6f}_{int(row['RepeatId'])+1}"
    # make sure to convert to newick format first using smith/scripts/dot_to_newick.py
    tree = Bio.Phylo.read(cur_folder / 'bin_tree.new', 'newick')
    normal_name = [x.name for x in tree.get_terminals() if x.name.split('-')[0] == '0'][0]
    tree.root_with_outgroup(normal_name)

    for clade in tree.get_nonterminals():
        if clade.name is not None and int(clade.name.split('-')[0]) not in fish_plot_data[(row['Confinement_global'], row['Confinement_local'])][0].index:
            ancestor = tree.get_path(clade)[-2]
            ancestor.clades.remove(clade)
            ancestor.clades.extend(clade.clades)

    tree_data[(row['Confinement_global'], row['Confinement_local'])] = tree

with open(DATA_DIR / 'tree_data.pkl', 'wb') as f:
    pickle.dump(tree_data, f)


# %%
# Noble et al. 2022 (real and similated data)
NOBLE_REPO_DIR = ARTICLE_FIGURES_DIR.parent.parent / 'ModesOfEvolution'
real_data = pd.read_csv(NOBLE_REPO_DIR / 'real_data.csv', index_col=0)

real_data['type'] = 'solid'
real_data.loc[real_data['dataset'] == 'AML', 'type'] = 'non-spatial'
real_data = real_data.loc[real_data['minimal'] == 0]
real_data = real_data[['dataset', 'n', 'D']]

noble_data_for_metric_plots = pd.read_csv(NOBLE_REPO_DIR / 'dataForMetricPlots.csv')
noble_combined_cases = pd.read_csv(NOBLE_REPO_DIR / 'DivMutation_Allcombined_cases.csv', sep=" ")

noble_data_for_metric_plots['label'] = 'invasive_glandular'
noble_data_for_metric_plots['Drivers'] += 1
noble_combined_cases['Drivers'] += 1
case_dict = {"caseA": "non-spatial", "caseB": "gland fission", "caseC": "invasive glandular",
             "caseD_new": "boundary growth", "neutral": "neutral"}
noble_combined_cases['label'] = noble_combined_cases['case'].map(case_dict)
noble_combined_cases['DriverDiversity'] = noble_combined_cases['Diversity']

noble_combined = pd.concat([noble_data_for_metric_plots,
                           noble_combined_cases.loc[noble_combined_cases['label'] == 'non-spatial']])
noble_combined['Sweep'] = noble_combined['DriverDiversity'] < 1.5

real_data.to_pickle(DATA_DIR / 'noble_2022_real_data.pkl')
noble_combined.to_pickle(DATA_DIR / 'noble_2022_simulations.pkl')
