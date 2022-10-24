import argparse
import random
import sys
import math
import logging
import os

import numpy as np
import pandas as pd
from PIL import Image
from matplotlib import cm, pyplot as plt


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("-i", "--input_file", type=str, default=None, required=True)
    parser.add_argument("-o", "--output_folder", type=str, default=None, required=False)
    args = parser.parse_args()

    if args.output_folder is not None:
        output_folder = args.output_folder
    else:
        output_folder = os.path.abspath(os.path.join(os.path.dirname(__file__), '../out'))

    vaf = pd.read_csv(args.input_file)
    plt.xlim([0.0,1.0])
    plt.hist(vaf["ccf"], 20, weights=vaf["pop"])    
    plt.xlabel("CCF")
    plt.ylabel("total population")
    plt.savefig(os.path.join(output_folder, "CCF.png"))