#!/usr/bin/env python3
import argparse
import os
import pickle
import re

import pandas as pd
from pyfish.core import process_data


CFG_RE = re.compile(
    r"^(?P<mut>\d+\.\d+)_(?P<fit>\d+\.\d+)_(?P<global>\d+\.\d+)_(?P<local>\d+\.\d+)_(?P<repeat>\d+)_sim_params\.json$"
)


def parse_cfg(filename):
    match = CFG_RE.match(filename)
    if match is None:
        raise ValueError(f"Unexpected config filename format: {filename}")
    return {
        "mutation": float(match.group("mut")),
        "fitness": float(match.group("fit")),
        "global_conf": float(match.group("global")),
        "local_conf": float(match.group("local")),
        "repeat": int(match.group("repeat")),
        "stub": filename[: -len("_sim_params.json")],
    }


def load_run_data(results_dir, run_stub):
    run_dir = os.path.join(results_dir, f"parameter_range_{run_stub}")
    pop_path = os.path.join(run_dir, "populations.csv")
    tree_path = os.path.join(run_dir, "parent_tree.csv")

    if not os.path.exists(pop_path) or not os.path.exists(tree_path):
        raise FileNotFoundError(
            "Missing simulation output for "
            f"parameter_range_{run_stub}. "
            "Expected populations.csv and parent_tree.csv."
        )

    populations_df = pd.read_csv(pop_path)
    parent_tree_df = pd.read_csv(tree_path)
    return populations_df, parent_tree_df


def build_fish_pickles(results_dir, data_dir, fish_cfg_dir):
    fish_plot_data = {}
    fish_cfgs = sorted(
        f for f in os.listdir(fish_cfg_dir) if f.endswith("_sim_params.json")
    )

    for cfg_name in fish_cfgs:
        parsed = parse_cfg(cfg_name)
        populations_df, parent_tree_df = load_run_data(results_dir, parsed["stub"])
        fish_plot_data[(parsed["global_conf"], parsed["local_conf"])] = process_data(
            populations_df, parent_tree_df, absolute=False, smooth=0
        )

    fish_out = os.path.join(data_dir, "fish_plot_data.pkl")
    with open(fish_out, "wb") as handle:
        pickle.dump(fish_plot_data, handle)
    print(f"Wrote {fish_out} with {len(fish_plot_data)} confinement combinations")


def build_trajectory_pickles(results_dir, data_dir, traj_cfg_dir):
    # Keep index mapping consistent with create_figures.ipynb.
    example_trajectories = {
        (0.0, 0.0): [0, 25, 97, 74],
        (0.5, 0.0): [67, 33, 54, 56],
        (0.0, 0.0625): [49, 1, 36, 98],
        (0.5, 0.25): [97, 24, 2, 12],
    }

    trajectories = {}
    known_cfgs = {
        cfg_name: parse_cfg(cfg_name)
        for cfg_name in os.listdir(traj_cfg_dir)
        if cfg_name.endswith("_sim_params.json")
    }

    for (global_conf, local_conf), repeats in example_trajectories.items():
        for idx, repeat_id in enumerate(repeats):
            # Config files use 1-based repeat id in their filename.
            expected_name = (
                f"{1e-5:.6f}_{0.1:.6f}_{global_conf:.6f}_{local_conf:.6f}_{repeat_id + 1}_sim_params.json"
            )

            if expected_name not in known_cfgs:
                raise FileNotFoundError(
                    "Expected trajectories config is missing: "
                    f"{os.path.join(traj_cfg_dir, expected_name)}"
                )

            parsed = known_cfgs[expected_name]
            full_population, parent_tree_df = load_run_data(results_dir, parsed["stub"])
            fish_data = process_data(full_population, parent_tree_df, absolute=False, smooth=0)
            population_df = fish_data[0].groupby("Id").sum()
            trajectories[(global_conf, local_conf, idx)] = (full_population, population_df)

    traj_out = os.path.join(data_dir, "trajectories.pkl")
    with open(traj_out, "wb") as handle:
        pickle.dump(trajectories, handle)
    print(f"Wrote {traj_out} with {len(trajectories)} trajectory entries")


def parse_args():
    parser = argparse.ArgumentParser(
        description="Build article reproduction pickle files from generated simulation outputs."
    )
    parser.add_argument("--results-dir", required=True)
    parser.add_argument("--data-dir", required=True)
    parser.add_argument("--fish-cfg-dir", required=True)
    parser.add_argument("--traj-cfg-dir", required=True)
    parser.add_argument("--only", choices=["all", "fish", "trajectories"], default="all")
    return parser.parse_args()


def main():
    args = parse_args()

    if args.only in ("all", "fish"):
        build_fish_pickles(args.results_dir, args.data_dir, args.fish_cfg_dir)

    if args.only in ("all", "trajectories"):
        build_trajectory_pickles(args.results_dir, args.data_dir, args.traj_cfg_dir)


if __name__ == "__main__":
    main()
