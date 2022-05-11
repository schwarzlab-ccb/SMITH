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
    parser.add_argument("-i", "--input_folder", type=str, default=None, required=False)
    parser.add_argument("-o", "--output_folder", type=str, default=None, required=False)
    parser.add_argument("--metrics", type=str, default=None, required=False)
    parser.add_argument("--generation", type=int, default=None, required=False)
    parser.add_argument("--over_time", action='store_true')
    args = parser.parse_args()

    if args.input_folder is not None:
        input_folder = args.input_folder
    else:
        input_folder = os.path.abspath(os.path.join(os.path.dirname(__file__), '../out'))

    if args.output_folder is not None:
        output_folder = args.output_folder
    else:
        output_folder = os.path.abspath(os.path.join(os.path.dirname(__file__), '../out'))

    if args.metrics is not None:
        metrics = args.metrics.split(',')
    else:
        metrics = DEFAULT_METRICS

    summary_df = pd.read_csv(os.path.join(input_folder, "summary.csv"), index_col=None)

    if not args.over_time:
        if args.generation is not None:
            cur_generation = args.generation
        else:
            cur_generation = np.max(summary_df['GenerationId'].values)
        summary_df = summary_df.loc[summary_df['GenerationId']==cur_generation]

    else:
        metrics += ['GenerationId']

    # fig, ax = plt.subplots(figsize=(20, 20))
    if args.over_time:
        summary_df['GenerationId'] = "$10^" + np.log10(summary_df['GenerationId']).astype(int).astype(str) + "$"
        g = sns.PairGrid(summary_df[metrics], hue='GenerationId', palette='viridis')
    else:
        g = sns.PairGrid(summary_df[metrics])
    g.map_upper(sns.scatterplot)

    # catch error if not enough datapoints for kdeplot are present
    try:
        g.map_lower(sns.kdeplot)
    except:
        pass

    g.map_diag(sns.histplot)

    if args.over_time:
        g.add_legend()

    plt.savefig(os.path.join(output_folder, f'metrics_relationships{"_over_time" if args.over_time else ""}.png'))
    plt.close()
