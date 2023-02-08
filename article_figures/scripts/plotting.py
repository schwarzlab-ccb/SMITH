import itertools
import os

import matplotlib.pyplot as plt
import matplotlib as mpl
import numpy as np
import pandas as pd
import seaborn as sns
from scipy.stats import ttest_ind
import matplotlib.transforms as mtransforms

# install PyFish via https://bitbucket.org/schwarzlab/pyfish
from pyfish import process_data, fish_plot
from pyfish.core import *

RESULTS_DIR = '../../results/experiments/results'
FINAL_RESULTS_DIR = '../../results/experiments/final_results'

plotting_params = {
    'WIDTH_FULL': 12,
    'WIDTH_HALF': 6,
    'HEIGHT_FULL': 18,
    'ASPECT_RATIO': 4/3,
    'FONTSIZE_HUGE': 20,
    'FONTSIZE_LARGE': 12,
    'FONTSIZE_MEDIUM': 10,
    'FONTSIZE_SMALL': 8,
    'FONTSIZE_TINY': 5,
    'LINEWIDTH': 3,
    'MARKERSIZE_SMALL': 3,
    'MARKERSIZE_MEDIUM': 5,
    'MARKERSIZE_LARGE': 10,
    'LINEWIDTH_SMALL': 1}


def set_plotting_params():
    plt.rc('font', family='sans-serif')
    plt.rc('font', size=plotting_params['FONTSIZE_MEDIUM'])
    plt.rc('axes', titlesize=plotting_params['FONTSIZE_LARGE'])
    plt.rc('axes', labelsize=plotting_params['FONTSIZE_MEDIUM'])
    plt.rc('xtick', labelsize=plotting_params['FONTSIZE_SMALL'])
    plt.rc('ytick', labelsize=plotting_params['FONTSIZE_SMALL'])
    plt.rc('legend', fontsize=plotting_params['FONTSIZE_SMALL'])
    plt.rc('legend', frameon=False)
    plt.rc('figure', titlesize=plotting_params['FONTSIZE_LARGE'])

    # Seaborn standard palette and theme
    sns.set_palette(sns.color_palette())
    sns.set_theme(style="white",
                  rc={"axes.facecolor": (0, 0, 0, 0), "legend.facecolor": "white",
                      'xtick.bottom': True, 'ytick.left': True,
                      "xtick.labelsize": 10, "ytick.labelsize": 10, 'axes.labelsize': 12, 'axes.titlesize': 12},)

    plt.rcParams['axes.spines.right'] = False
    plt.rcParams['axes.spines.top'] = False


def stylize_axes(ax):
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)


def label_axes(axs, fontsize=15):
    letters = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q',
               'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z']

    for ax, l in zip(axs.flat, letters):
        stylize_axes(ax)
        trans = mtransforms.ScaledTranslation(-20/72, 7/72, plt.gcf().dpi_scale_trans)
        ax.text(0.0, 1.0, l, transform=ax.transAxes + trans,
                fontsize=fontsize, color='black', va='bottom', ha='left', fontfamily='serif', fontweight='bold')


def plot_statistical_significance(ax, data, c, parameter, only_neighbors=False, h_scale=0.1, hl_scale=0.1, yshift=3.5, ystart=None):
    xticklabels = {x.get_position()[0]: x.get_text() for x in ax.get_xticklabels()}
    xticks = ax.get_xticks()

    if only_neighbors:
        xs = [(xticks[i], xticks[i+1]) for i in range(len(xticks)-1)]
    else:
        xs = itertools.combinations(xticks, 2)

    max_y = ax.get_ylim()[1]
    h = (max_y*h_scale)
    hl = (max_y*hl_scale)
    if ystart is not None:
        y = ystart
    else:
        y = ax.get_ylim()[1]
    for x1, x2 in xs:
        x1_label = xticklabels[x1]
        x2_label = xticklabels[x2]
        max_y = ax.get_ylim()[1]
        y = y + h * yshift
        p_value = ttest_ind(data.loc[data[parameter].astype(str) == x1_label, c],
                            data.loc[data[parameter].astype(str) == x2_label, c]).pvalue
        cur_text = "***" if p_value < 0.0005 else ("**" if p_value <
                                                   0.005 else ("*" if p_value < 0.05 else "n.s."))
        ax.plot([x1, x1, x2, x2], [y, y+hl, y+hl, y], lw=1.5, c='black')
        ax.text((x1+x2)*.5, y + 0.1 * h * yshift, cur_text,
                ha='center', va='bottom', color='black', fontsize=10)
    ax.set_ylim(ax.get_ylim()[0], ax.get_ylim()[1]+h)


def load_final_confinement_revisions(absolute=False, selection_fish=[]):
    
    target_generation = 20
    confinement_data_fish = []
    lines = []

    # range_MutationProb = [0.000005, 0.00001, 0.00002, 0.00004]
    # range_FitnessMean = [0.05, 0.1, 0.2, 0.4]
    range_MutationProb = [0.000001, 0.000005, 0.00001 , 0.00005, 0.0001]
    range_FitnessMean = [0.01, 0.05, 0.1, 0.15, 0.2]
    range_Confinement_global = [0, 0.0625, 0.125, 0.25, 0.5, 1]
    range_Confinement_local = [0, 0.0625, 0.125, 0.25, 0.5, 1]

    target_generation = 20

    long_results = pd.DataFrame(columns=['MeanDriversPerCell', 'ClonalDiversity',
                                            'fraction_alive_cells', 'fraction_necro', 'RepeatId', 'MutationProb', 'FitnessMean', 'Confinement_global', 'Confinement_local'])


    for cur_MutationProb in range_MutationProb:
        for cur_FitnessMean in range_FitnessMean:
            for cur_Confinement_global in range_Confinement_global:
                for cur_Confinement_local in range_Confinement_local:
                    cur_file = f'{FINAL_RESULTS_DIR}/results_summary/parameter_range_{cur_MutationProb:.6f}_{cur_FitnessMean:.6f}_{cur_Confinement_global:.6f}_{cur_Confinement_local:.6f}.csv'
                    if not os.path.exists(cur_file):
                        continue
                    cur = pd.read_csv(cur_file)
                    cur = cur.loc[cur['GenerationId'] == target_generation]
                    cur['fraction_alive_cells'] = cur['CellAliveCount'] / (cur['CellTotalCount'])
                    cur['fraction_necro'] = cur['CellNecroCount'] / (cur['CellTotalCount'])

                    cur['MutationProb'] = cur_MutationProb
                    cur['FitnessMean'] = cur_FitnessMean
                    cur['Confinement_global'] = cur_Confinement_global
                    cur['Confinement_local'] = cur_Confinement_local

                    long_results = pd.concat([long_results, cur[['MeanDriversPerCell', 'ClonalDiversity',
                                                                 'fraction_alive_cells', 'fraction_necro', 'RepeatId', 'MutationProb', 'FitnessMean', 'Confinement_global', 'Confinement_local']].reset_index(drop=True)]).reset_index(drop=True)


        
    return long_results, confinement_data_fish, lines



def load_final_confinement_revisions_all(absolute=False, selection_fish=[]):
    
    target_generation = 20
    confinement_data_fish = []
    lines = []

    cur_dir = f'{FINAL_RESULTS_DIR}/results_summary'
    target_generation = 20

    columns = [
        'MeanDriversPerCell', 'ClonalDiversity', 'fraction_alive_cells', 'Generations',
        'fraction_necro', 'RepeatId', 'MutationProb', 'FitnessMean', 'Confinement_global', 'Confinement_local']

    long_results = pd.DataFrame(columns=columns)

    for f in [x for x in os.listdir(cur_dir) if x[:5]=='param']:
        cur_file = os.path.join(cur_dir, f)
        if not os.path.exists(cur_file):
            continue

        cur_MutationProb = float(f.split('_')[2])
        cur_FitnessMean = float(f.split('_')[3])
        cur_Confinement_global = float(f.split('_')[4])
        cur_Confinement_local = float(f.split('_')[5].replace('.csv', ''))

        cur = pd.read_csv(cur_file)
        cur = cur.loc[cur['GenerationId'] == target_generation]
        cur['fraction_alive_cells'] = cur['CellAliveCount'] / (cur['CellTotalCount'])
        cur['fraction_necro'] = cur['CellNecroCount'] / (cur['CellTotalCount'])

        cur['MutationProb'] = cur_MutationProb
        cur['FitnessMean'] = cur_FitnessMean
        cur['Confinement_global'] = cur_Confinement_global
        cur['Confinement_local'] = cur_Confinement_local

        long_results = pd.concat([long_results, cur[columns].reset_index(drop=True)]).reset_index(drop=True)

    return long_results, confinement_data_fish, lines

    
def plot_final_fish(data, ax, lines=None):
    fish_plot(pops_stack=data[0], steps=data[1], colors=data[2], pop_max=data[3], ax=ax)
    max_x = ax.get_xlim()[1]
    ax.set_xticks(np.arange(0, 1.25 * max_x, 0.25 * max_x))
    ax.set_xticklabels(np.arange(0, 1.25, 0.25))
    
    ax.set_xlabel('relative simulation time')

    if lines is not None:
        for l in lines:
            ax.axvline(l, color='black', linestyle='--', alpha=0.25)
    


def load_final_metrics_over_time_revisions(cur_MutationProb=0.0001,
                                          cur_FitnessMean=0.2,
                                          cur_Confinement_global=0.0,
                                           cur_Confinement_local=0.0
                                          ):
    
    target_generation = 20
    cur_file = f'{FINAL_RESULTS_DIR}/results_summary/parameter_range_{cur_MutationProb:.6f}_{cur_FitnessMean:.6f}_{cur_Confinement_global:.6f}_{cur_Confinement_local:.6f}.csv'
    fitness_comparison_df = pd.read_csv(cur_file)

    brackets = "{", "}"
    for m, mr in zip(["clonal diversity", "mean drivers per cell"], ['ClonalDiversity', 'MeanDriversPerCell']):
        fitness_comparison_df[m] = fitness_comparison_df[mr]
        fitness_comparison_df['Generation_label'] = fitness_comparison_df['GenerationId'].apply(
        lambda x: f'$2^{brackets[0]}{int(x)+10}{brackets[1]}$')
            
    return fitness_comparison_df


def load_single_fish_data(cur_MutationProb, cur_FitnessMean, cur_Confinement_global, cur_Confinement_local, r):

    cur_folder = f'{RESULTS_DIR}/parameter_range_{cur_MutationProb:.6f}_{cur_FitnessMean:.6f}_{cur_Confinement_global:.6f}_{cur_Confinement_local:.6f}_{int(r)+1}'

    populations_df = pd.read_csv(f'{cur_folder}/populations.csv')
    parent_tree_df = pd.read_csv(f'{cur_folder}/parent_tree.csv')
    fish_data = process_data(populations_df, parent_tree_df, absolute=False, smooth=0)

    return fish_data
    

def load_final_fitness_revisions_all():

    key_FitnessDist = {0: "Constant", 1: "Normal", 2: "Exponential", 3: "Uniform"}
    key_FitnessAcc = {0: "Mul", 1: "Add", 2: "ETH"}

    target_generation = 20
    
    all_results = []
    cur_dir = f'{FINAL_RESULTS_DIR}/results_summary'

    for f in [x for x in os.listdir(cur_dir) if x[:10]=='fitness_pa']:
        cur_file = os.path.join(cur_dir, f)
        # if not os.path.exists(cur_file):
        #     continue

        cur_FitnessDist = float(f.split('_')[3])
        cur_FitnessAcc = float(f.split('_')[4])
        cur_MutationProb = float(f.split('_')[5])
        cur_FitnessMean = float(f.split('_')[6])
        cur_Confinement_global = float(f.split('_')[7])
        cur_Confinement_local = float(f.split('_')[8].replace('.csv', ''))

        cur = pd.read_csv(cur_file)
        cur = cur.loc[cur['GenerationId'] == target_generation]

        cur['FitnessDist'] = cur_FitnessDist
        cur['FitnessAcc'] = cur_FitnessAcc
        cur['MutationProb'] = cur_MutationProb
        cur['FitnessMean'] = cur_FitnessMean
        cur['Confinement_global'] = cur_Confinement_global
        cur['Confinement_local'] = cur_Confinement_local


        all_results.append(
            cur[['MeanDriversPerCell', 'ClonalDiversity', 'MutationProb',
                'FitnessMean', 'Confinement_global', 'Confinement_local',
                'FitnessDist', 'FitnessAcc', 'RepeatId']].reset_index(drop=True))

    long_results = pd.concat(all_results).reset_index(drop=True)
        
    long_results['FitnessDist'] = long_results['FitnessDist'].apply(lambda x: key_FitnessDist[x])
    long_results['FitnessAcc'] = long_results['FitnessAcc'].apply(lambda x: key_FitnessAcc[x])

    return long_results
    
    
def plot_final_fitness(i, data, ax, h_scale=0.1, hl_scale=0.1, yshift=3.5, col='FitnessDist', order=None):
    cols = ['MeanDriversPerCell', 'ClonalDiversity']
    cols_formatted = ["mean drivers per cell", "clonal diversity"]
    
    if col == 'FitnessDist':
        if order is None:
            order = ['Exponential', 'Constant', 'Normal', 'Uniform']
        ax.set_xlabel('fitness distribution')    
    elif col == 'FitnessAcc':
        if order is None:
            order = ['Add', 'Mul', 'ETH']
        ax.set_xlabel('fitness accumulation')    
    sns.boxplot(data=data, y=cols[i], x=col, ax=ax, order=order)
    yticks = ax.get_yticks()
    spine_height = ax.get_ylim()[1]
    plot_statistical_significance(ax, data, cols[i], col, h_scale=h_scale, hl_scale=hl_scale, yshift=yshift)
    ax.set_yticks(yticks)
    ax.set_ylim(np.min(data[cols[i]]), ax.get_ylim()[1])  
    ax.spines['left'].set_bounds((np.min(data[cols[i]]), yticks[-1]))
    # ax.yaxis.set_label_coords(-0.1, 0.5 * yticks[-1] / ax.get_ylim()[1])
    ax.set_ylabel(cols_formatted[i], ha='center')
    

########### MODIFIED FROM MEDICC ##################
COL_ALLELE_A = mpl.colors.to_rgba('orange')
COL_ALLELE_B = mpl.colors.to_rgba('teal')
COL_CLONAL = mpl.colors.to_rgba('lightgrey')
COL_NORMAL = mpl.colors.to_rgba('dimgray')
COL_GAIN = mpl.colors.to_rgba('red')
COL_WGD = mpl.colors.to_rgba('green')
COL_LOSS = mpl.colors.to_rgba('blue')
COL_CHR_LABEL = mpl.colors.to_rgba('grey')
COL_VLINES = '#1f77b4'
COL_MARKER_INTERNAL = COL_VLINES
COL_MARKER_TERMINAL = 'black'
COL_MARKER_NORMAL = 'green'
COL_SUMMARY_LABEL = 'grey'
COL_BACKGROUND = 'white'
COL_BACKGROUND_HATCH = 'lightgray'
COL_PATCH_BACKGROUND = 'white'
LINEWIDTH_COPY_NUMBERS = 2
LINEWIDTH_CHR_BOUNDARY = 1
LINEWIDTH_SEGMENT_BOUNDARY = 0.5
ALPHA_PATCHES = 0.15
ALPHA_PATCHES_WGD = 0.3
ALPHA_CLONAL = 0.3
BACKGROUND_HATCH_MARKER = '/////'
TREE_MARKER_SIZE = 40
YLABEL_FONT_SIZE = 8
YLABEL_TICK_SIZE = 6
XLABEL_FONT_SIZE = 10
XLABEL_TICK_SIZE = 8
CHR_LABEL_SIZE = 8
SMALL_SEGMENTS_LIMIT = 1e7


def _get_x_positions(tree):
    """Create a mapping of each clade to its horizontal position.
    Dict of {clade: x-coord}
    """
    depths = tree.depths()
    # If there are no branch lengths, assume unit branch lengths
    if not max(depths.values()):
        depths = tree.depths(unit_branch_lengths=True)
    return depths


def _get_y_positions(tree, adjust=False, normal_name='diploid'):
    """Create a mapping of each clade to its vertical position.
    Dict of {clade: y-coord}.
Coordinates are negative, and integers for tips.
    """
    maxheight = tree.count_terminals()
    heights = {tip: maxheight -1 -i for i,
            tip in enumerate(reversed([x for x in tree.get_terminals() if x.name != normal_name]))}
    heights.update({list(tree.find_clades(normal_name))[0]: maxheight})

    # Internal nodes: place at midpoint of children
    def calc_row(clade):
        for subclade in clade:
            if subclade not in heights:
                calc_row(subclade)
        heights[clade] = (heights[clade.clades[0]] + heights[clade.clades[-1]]) / 2.0

    if tree.root.clades:
        calc_row(tree.root)

    diploid_height = heights[[x for x in tree.get_terminals() if x.name.split('-')[0] == '0'][0]]
    for clade in tree.find_clades():
        if clade.name is None:
            heights[clade] = diploid_height

    return heights


def plot_tree(input_tree,
              label_func=None,
              title='',
              ax=None,
              output_name=None,
              normal_name='diploid',
              width_scale=1,
              height_scale=1,
              show_branch_lengths=True,
              show_branch_support=False,
              show_events=False,
              branch_labels=None,
              label_colors=None,
              hide_internal_nodes=False,
              marker_size=None,
              marker_type=None,
              line_width=None,
              alive=None,
              **kwargs):
    """Plot the given tree using matplotlib (or pylab).
    The graphic is a rooted tree, drawn with roughly the same algorithm as
    draw_ascii.
    Additional keyword arguments passed into this function are used as pyplot
    options. The input format should be in the form of:
    pyplot_option_name=(tuple), pyplot_option_name=(tuple, dict), or
    pyplot_option_name=(dict).
    Example using the pyplot options 'axhspan' and 'axvline'::
        from Bio import Phylo, AlignIO
        from Bio.Phylo.TreeConstruction import DistanceCalculator, DistanceTreeConstructor
        constructor = DistanceTreeConstructor()
        aln = AlignIO.read(open('TreeConstruction/msa.phy'), 'phylip')
        calculator = DistanceCalculator('identity')
        dm = calculator.get_distance(aln)
        tree = constructor.upgma(dm)
        Phylo.draw(tree, axhspan=((0.25, 7.75), {'facecolor':'0.5'}),
        ... axvline={'x':0, 'ymin':0, 'ymax':1})
    Visual aspects of the plot can also be modified using pyplot's own functions
    and objects (via pylab or matplotlib). In particular, the pyplot.rcParams
    object can be used to scale the font size (rcParams["font.size"]) and line
    width (rcParams["lines.linewidth"]).
    :Parameters:
        label_func : callable
            A function to extract a label from a node. By default this is str(),
            but you can use a different function to select another string
            associated with each node. If this function returns None for a node,
            no label will be shown for that node.
        do_show : bool
            Whether to show() the plot automatically.
        show_support : bool
            Whether to display confidence values, if present on the tree.
        ax : matplotlib/pylab axes
            If a valid matplotlib.axes.Axes instance, the phylogram is plotted
            in that Axes. By default (None), a new figure is created.
        branch_labels : dict or callable
            A mapping of each clade to the label that will be shown along the
            branch leading to it. By default this is the confidence value(s) of
            the clade, taken from the ``confidence`` attribute, and can be
            easily toggled off with this function's ``show_support`` option.
            But if you would like to alter the formatting of confidence values,
            or label the branches with something other than confidence, then use
            this option.
        label_colors : dict or callable
            A function or a dictionary specifying the color of the tip label.
            If the tip label can't be found in the dict or label_colors is
            None, the label will be shown in black.
    """

    import matplotlib.collections as mpcollections
    if ax is None:
        nsamp = len(list(input_tree.find_clades()))
        plot_height = height_scale * nsamp * 0.25
        max_leaf_to_root_distances = np.max([np.sum([x.branch_length for x in input_tree.get_path(leaf)])
                            for leaf in input_tree.get_terminals()])
        plot_width = 5 + np.max([0, width_scale * np.log10(max_leaf_to_root_distances / 100) * 5])

        # maximum figure size is 250x250 inches
        fig, ax = plt.subplots(figsize=(min(250, plot_width), min(250, plot_height)))

    label_func=label_func if label_func is not None else lambda x: x

    # options for displaying label colors.
    if label_colors is not None:
        if callable(label_colors):
            def get_label_color(label):
                return label_colors(label)
        else:
            # label_colors is presumed to be a dict
            def get_label_color(label):
                return label_colors.get(label, "black")
    else:
        clade_colors = {}
        for sample in [x.name for x in list(input_tree.find_clades(''))]:
            ## determine if sample is terminal
            is_terminal = True
            matches = list(input_tree.find_clades(sample))
            if len(matches) > 0:
                clade = matches[0]
                is_terminal = clade.is_terminal()
            ## determine if sample is normal
            clade_colors[sample] = COL_MARKER_TERMINAL
            if not is_terminal:
                clade_colors[sample] = COL_MARKER_INTERNAL
            if sample == normal_name:
                clade_colors[sample] = COL_MARKER_NORMAL
        
        def get_label_color(label):
            return clade_colors.get(label, "black")

    if marker_size is None:
        marker_size = TREE_MARKER_SIZE
    elif callable(marker_size):
        marker_func=lambda x: (marker_size(x.name), get_label_color(x.name)) if x.name is not None else None
    elif type(marker_size) is dict:
        marker_func=lambda x: (marker_size[x.name], get_label_color(x.name)) if x.name is not None else None
    else:
        marker_func=lambda x: (marker_size, get_label_color(x.name)) if x.name is not None else None


    ax.axes.get_yaxis().set_visible(False)
    ax.spines["right"].set_visible(False)
    ax.spines["left"].set_visible(False)
    ax.spines["top"].set_visible(False)
    ax.xaxis.set_major_locator(mpl.ticker.MaxNLocator(integer=True, prune=None))
    ax.xaxis.set_tick_params(labelsize=XLABEL_TICK_SIZE)
    ax.xaxis.label.set_size(XLABEL_FONT_SIZE)
    ax.set_title(title, x=0.01, y=1.0, ha='left', va='bottom',
                fontweight='bold', fontsize=16, zorder=10)
    x_posns = _get_x_positions(input_tree)
    y_posns = _get_y_positions(
        input_tree, adjust=not hide_internal_nodes, normal_name=normal_name)

    # Arrays that store lines for the plot of clades
    horizontal_linecollections = []
    vertical_linecollections = []

    # Options for displaying branch labels / confidence
    def value_to_str(value):
        if value is None or value == 0:
            return None
        elif int(value) == value:
            return str(int(value))
        else:
            return str(value)

    if not branch_labels:
        if show_branch_lengths:
            def format_branch_label(x): 
                return value_to_str(np.round(x.branch_length, 1)) if x.name != 'root' and x.name is not None else None
        else:
            def format_branch_label(clade):
                return None

    elif isinstance(branch_labels, dict):
        def format_branch_label(clade):
            return branch_labels.get(clade)
    else:
        if not callable(branch_labels):
            raise TypeError(
                "branch_labels must be either a dict or a callable (function)"
            )
        def format_branch_label(clade):
            return value_to_str(branch_labels(clade))

    if show_branch_support:
        def format_support_value(clade):
            if clade.name == 'root' or clade.name is None:
                return None
            try:
                confidences = clade.confidences
            # phyloXML supports multiple confidences
            except AttributeError:
                pass
            else:
                return "/".join(value_to_str(cnf.value) for cnf in confidences)
            if clade.confidence is not None:
                return value_to_str(clade.confidence)
            return None


    def draw_clade_lines(
        use_linecollection=False,
        orientation="horizontal",
        y_here=0,
        x_start=0,
        x_here=0,
        y_bot=0,
        y_top=0,
        color="black",
        lw=".1",
        linestyle='-',
        alpha=1.0,
    ):
        """Create a line with or without a line collection object.
        Graphical formatting of the lines representing clades in the plot can be
        customized by altering this function.
        """
        if not use_linecollection and orientation == "horizontal":
            ax.hlines(y_here, x_start, x_here, color=color, alpha=alpha, lw=lw, zorder=1, linestyle=linestyle)
        elif use_linecollection and orientation == "horizontal":
            horizontal_linecollections.append(
                mpcollections.LineCollection(
                    [[(x_start, y_here), (x_here, y_here)]], color=color, alpha=alpha, lw=lw, linestyle=linestyle
                )
            )
        elif not use_linecollection and orientation == "vertical":
            ax.vlines(x_here, y_bot, y_top, color=color, alpha=alpha, zorder=1, linestyle=linestyle)
        elif use_linecollection and orientation == "vertical":
            vertical_linecollections.append(
                mpcollections.LineCollection(
                    [[(x_here, y_bot), (x_here, y_top)]], color=color, alpha=alpha, lw=lw, linestyle=linestyle
                )
            )

    def draw_clade(clade, x_start, color, lw):
        """Recursively draw a tree, down from the given clade."""
        x_here = x_posns[clade]
        y_here = y_posns[clade]
        # phyloXML-only graphics annotations
        if hasattr(clade, "color") and clade.color is not None:
            color = clade.color.to_hex()
        if hasattr(clade, "width") and clade.width is not None:
            lw = clade.width * plt.rcParams["lines.linewidth"]
        # Draw a horizontal line from start to here
        if alive is None or clade.name is None:
            linestyle = '-'
            alpha = 1.0
        else:
            if alive[int(clade.name.split('-')[0])]:
                linestyle = '-'
                alpha = 1.0
            else:
                linestyle = '--'
                alpha = 0.25

        draw_clade_lines(
            use_linecollection=True,
            orientation="horizontal",
            y_here=y_here,
            x_start=x_start,
            x_here=x_here,
            color=color,
            lw=lw,
            linestyle=linestyle,
            alpha=alpha
        )
        # Add node marker
        if marker_func is not None:
            marker = marker_func(clade)
            if marker is not None and clade is not None and not(hide_internal_nodes and not clade.is_terminal()):
                cur_marker_size, marker_col = marker_func(clade)
                if marker_type is None:
                    marker = 'o'
                else:
                    marker = marker_type(clade.name)
                
                if alive is not None:
                    if clade.name is not None and '-' in clade.name:
                        if alive[int(clade.name.split('-')[0])]:
                            marker = 'o'
                        else:
                            marker = 'X'
                            if type(marker_col) != str:
                                marker_col[-1] = 0.33
                    else:
                        marker = 'D'

                ax.plot(x_here, y_here, linestyle='none', marker=marker, markersize=cur_marker_size, color=marker_col, zorder=99)

        # Add node/taxon labels
        label = label_func(str(clade.name))
        ax_scale = ax.get_xlim()[1] - ax.get_xlim()[0]

        if label not in (None, clade.__class__.__name__) and \
                not (hide_internal_nodes and not clade.is_terminal()):
            ax.text(
                x_here + min(0.02*ax_scale, 1),
                y_here,
                " %s" % label,
                verticalalignment="center",
                color=get_label_color(label),
            )
        # Add label above the branch
        conf_label = format_branch_label(clade)
        if conf_label:
            ax.text(
                0.5 * (x_start + x_here),
                y_here - 0.15,
                conf_label,
                fontsize="small",
                horizontalalignment="center",
            )
        # Add support below the branch
        if show_branch_support:
            support_value = format_support_value(clade)
            if support_value:
                ax.text(
                    0.5 * (x_start + x_here),
                    y_here + 0.25,
                    support_value + '%',
                    fontsize="small",
                    color='grey',
                    horizontalalignment="center",
                )
        # Add Events list
        if show_events and clade.events is not None:
            ax.text(
                0.5 * (x_start + x_here),
                y_here - 0.15,
                clade.events,
                fontsize="small",
                color=COL_MARKER_NORMAL,
                horizontalalignment="center",
            )
        if clade.clades:
            # Draw a vertical line connecting all children
            y_mid = y_posns[clade]
            # Only apply widths to horizontal lines, like Archaeopteryx
            for child in clade.clades:
                y_top = y_posns[child]

                if alive is None or child.name is None:
                    linestyle = '-'
                    alpha = 1.0
                else:
                    if alive[int(child.name.split('-')[0])]:
                        linestyle = '-'
                        alpha = 1.0
                    else:
                        linestyle = '--'
                        alpha = 0.25
                draw_clade_lines(
                    use_linecollection=True,
                    orientation="vertical",
                    x_here=x_here,
                    y_bot=y_mid,
                    y_top=y_top,
                    color=color,
                    lw=lw,
                    linestyle=linestyle,
                    alpha=alpha
                )
                draw_clade(child, x_here, color, lw)
            




    if line_width is None:
        line_width = plt.rcParams["lines.linewidth"]
    draw_clade(input_tree.root, 0, "k", line_width)

    # If line collections were used to create clade lines, here they are added
    # to the pyplot plot.
    for i in horizontal_linecollections:
        ax.add_collection(i)
    for i in vertical_linecollections:
        ax.add_collection(i)

    ax.set_xlabel("branch length")
    ax.set_ylabel("taxa")

    # Add margins around the `tree` to prevent overlapping the ax
    xmax = max(x_posns.values())
    ymax = max(y_posns.values())
    #ax.set_xlim(-0.05 * xmax, 1.25 * xmax)
    ax.set_xlim(-0.05 * xmax, 1.05 * xmax)
    # Also invert the y-axis (origin at the top)
    ax.set_ylim(1.05 * ymax, -0.05 * ymax)

    # Parse and process key word arguments as pyplot options
    for key, value in kwargs.items():
        try:
            # Check that the pyplot option input is iterable, as required
            list(value)
        except TypeError:
            raise ValueError(
                'Keyword argument "%s=%s" is not in the format '
                "pyplot_option_name=(tuple), pyplot_option_name=(tuple, dict),"
                " or pyplot_option_name=(dict) " % (key, value)
            ) from None
        if isinstance(value, dict):
            getattr(plt, str(key))(**dict(value))
        elif not (isinstance(value[0], tuple)):
            getattr(plt, str(key))(*value)
        elif isinstance(value[0], tuple):
            getattr(plt, str(key))(*value[0], **dict(value[1]))

    if output_name is not None:
        plt.savefig(output_name + ".png", bbox_inches='tight')

    return plt.gcf()
