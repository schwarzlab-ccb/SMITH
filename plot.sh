#!/usr/bin/env bash

out=$1
[[ -z "$out" ]] && { out="./out"; }

if [[ ! -d "$out" ]]; then
    echo "Target folder $out does not exist."
    exit 1
fi

# The entry folder must include the run metadata files.
if [[ ! -f "$out/summary.csv" || ! -f "$out/sim_params.json" ]]; then
    echo "Target folder $out must contain summary.csv and sim_params.json."
    exit 1
fi

plot_folder() {
    local folder="$1"
    local col=42
    local smooth=2

    echo "Plotting folder $folder"

    if [[ -f "$folder/populations.csv" && -f "$folder/parent_tree.csv" ]]; then
        echo "Plotting Fish Plot"
        pyfish "$folder/populations.csv" "$folder/parent_tree.csv" "$folder/fish.png" -R "$col" -S "$smooth"
        pyfish "$folder/populations.csv" "$folder/parent_tree.csv" "$folder/fish_abs.png" -R "$col" -S "$smooth" -a
    else
        echo "File $folder/populations.csv or $folder/parent_tree.csv does not exist. Skipping fish plot."
    fi

    if [[ -f "$folder/clone_tree.dot" ]]; then
        echo "Plotting Clone Tree"
        dot -Tpng "$folder/clone_tree.dot" > "$folder/clone_tree.png"
    else
        echo "File $folder/clone_tree.dot does not exist. Skipping clone tree plot."
    fi

    if [[ -f "$folder/clone_tree.new" ]]; then
        echo "Plotting Newick Tree"
        python3 - "$folder/clone_tree.new" "$folder/phylogenetic_tree.png" <<'PY'
import sys
import matplotlib
import matplotlib.pyplot as plt
from Bio import Phylo

src, dst = sys.argv[1], sys.argv[2]
tree = Phylo.read(src, "newick")
fig, ax = plt.subplots(figsize=(12, max(6, tree.count_terminals() * 0.22)))
Phylo.draw(tree, axes=ax, do_show=False, label_func=lambda c: c.name or "")
ax.set_title("Clone Tree Through Simulation Time")
ax.set_xlabel("step")
fig.tight_layout()
fig.savefig(dst)
PY
    else
        echo "File $folder/clone_tree.new does not exist. Skipping Newick tree plot."
    fi
}

if [[ -f "$out/populations.csv" && -f "$out/parent_tree.csv" ]]; then
    plot_folder "$out"
else
    echo "No direct output files found in $out. Traversing child folders."
    found=0

    while IFS= read -r -d '' dir; do
        if [[ -f "$dir/populations.csv" && -f "$dir/parent_tree.csv" ]]; then
            plot_folder "$dir"
            found=1
        fi
    done < <(find "$out" -mindepth 1 -type d -print0)

    if [[ "$found" -eq 0 ]]; then
        echo "No child folders with populations.csv and parent_tree.csv were found under $out."
        exit 1
    fi
fi
