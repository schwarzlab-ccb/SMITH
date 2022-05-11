import argparse
import os

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from Bio import Phylo

from dot_to_newick import convert_dot_to_newick as dot_to_newick

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("-i", "--input_folder", type=str, default=None, required=False)
    parser.add_argument("-o", "--output_folder", type=str, default=None, required=False)
    args = parser.parse_args()

    if args.input_folder is not None:
        input_folder = args.input_folder
    else:
        input_folder = os.path.abspath(os.path.join(os.path.dirname(__file__), '../out'))

    if args.output_folder is not None:
        output_folder = args.output_folder
    else:
        output_folder = os.path.abspath(os.path.join(os.path.dirname(__file__), '../out'))

    populations_df = pd.read_csv(os.path.join(input_folder, "populations.csv"), index_col=None)

    # load tree
    if not 'parent_graph.new' in os.listdir(input_folder):
        if 'parent_graph.dot' in os.listdir(input_folder):
            dot_to_newick(os.path.join(input_folder, 'parent_graph.dot'),
                          os.path.join(input_folder, 'parent_graph.new'))
        else:
            raise BaseException(f"Did not find either 'parent_graph.new' or 'parent_graph.dot'"
                                "in input folder {input_folder}")

    tree = Phylo.read(os.path.join(input_folder, 'parent_graph.new'), 'newick')

    def calc_number_mutations(tree, clade):
        return np.sum([x.branch_length for x in tree.get_path(list(tree.find_clades(clade))[0])])

    number_mutations = {clade.name.split('-')[0]: calc_number_mutations(
        tree, clade) for clade in tree.find_clades() if clade.name is not None}

    generations = np.sort(np.unique(populations_df['Step']))

    mean_driver = np.zeros(len(generations))
    clonal_diversity = np.zeros(len(generations))
    for j, i in enumerate(generations):
        cur_pop = populations_df.loc[populations_df['Step']==i].copy()
        mean_driver[j] = (cur_pop['Drivers'] * cur_pop['Pop']).sum() / cur_pop['Pop'].sum()

        clonal_diversity[j] = 1 / np.sum((cur_pop['Pop'] / cur_pop['Pop'].sum())**2)


    fig, axs = plt.subplots(ncols=3, figsize=(20, 10))

    axs[0].plot(generations, mean_driver, 'o-')
    axs[0].set_xlabel('generation')
    axs[0].set_ylabel('mean nr drivers')
    axs[1].plot(generations, clonal_diversity, 'o-')
    axs[1].set_xlabel('generation')
    axs[1].set_ylabel('clonal diversity')
    axs[2].plot(mean_driver, clonal_diversity, 'o-')
    axs[2].set_xlabel('mean nr drivers')
    axs[2].set_ylabel('clonal diversity')
    plt.tight_layout()
    plt.savefig(os.path.join(output_folder, 'population_score.png'))
    plt.close()
