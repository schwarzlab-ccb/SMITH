import argparse
import itertools
import os

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import seaborn as sns

DEFAULT_METRICS = ["ClonalDiversity", "TreeBalance", "MeanDriversPerCell"]


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("-i", "--input_folders", required=True)
    parser.add_argument("-o", "--output_folder", type=str, default=None, required=False)
    parser.add_argument("--metrics", type=str, default=None, required=False)
    parser.add_argument("--generation", type=int, default=None, required=False)
    args = parser.parse_args()

    summary_df = pd.DataFrame()
    for cur_folder in args.input_folders.split(' '):

        cur = pd.read_csv(os.path.join(cur_folder, 'summary.csv'), index_col=None)
        # cur.iloc[(cur == np.nan).values] == -1
        cur['experiment'] = cur_folder.split('/')[-1]
        summary_df = pd.concat([summary_df, cur], axis=0)

    summary_df = summary_df.reset_index()

    if args.generation is not None:
        cur_generation = args.generation
    else:
        cur_generation = np.max(summary_df['GenerationId'].values)
    summary_df = summary_df.loc[summary_df['GenerationId']==cur_generation]

    if args.output_folder is not None:
        output_folder = args.output_folder
    else:
        output_folder = os.path.abspath(os.path.join(os.path.dirname(__file__), '../out'))

    if args.metrics is not None:
        metrics = args.metrics.split(',')
    else:
        metrics = DEFAULT_METRICS

    # aliveCount, totalCount, generations, treeDepth, nodeCount, leafCount, branching, subcloneTotal, subcloneSelect, clonalDiversityFiltered, clonalDiversity, treeBalancetreeBalanceFiltered, meanDriversPerCell, meanDriversPerCellFiltered

    # fig, ax = plt.subplots(figsize=(20, 20))
    g = sns.PairGrid(summary_df[metrics + ['experiment']], hue='experiment')
    g.map_upper(sns.scatterplot)


    # catch error if not enough datapoints for kdeplot are present
    try:
        g.map_lower(sns.kdeplot)
    except:
        pass

    g.map_diag(sns.histplot)
    g.add_legend()

    plt.savefig(os.path.join(output_folder, 'metrics_relationships_{}.png'.format('-'.join(summary_df['experiment'].unique()))))
    plt.close()
