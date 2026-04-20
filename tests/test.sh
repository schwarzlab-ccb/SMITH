#!/usr/bin/env bash

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
target_dir="$repo_root/tests/targets"
out_dir="$repo_root/tests/out"

rm -rf "$out_dir"
mkdir -p "$out_dir"

dotnet run -- -C "$target_dir/test_params.json" -O "$out_dir"
echo "$(tput setaf 7)"
echo "------------------------------------------------------"
echo "- COMPARE FILES --------------------------------------"
echo "------------------------------------------------------"
diff --strip-trailing-cr "$out_dir/clone_tree.dot" "$target_dir/clone_tree.dot" || echo "clone_tree.dot not matching"
diff --strip-trailing-cr "$out_dir/clone_tree.new" "$target_dir/clone_tree.new" || echo "clone_tree.new not matching"
diff --strip-trailing-cr "$out_dir/parent_tree.csv" "$target_dir/parent_tree.csv" || echo -e "$(tput setaf 1) \n - parent_tree.csv not matching"
diff --strip-trailing-cr "$out_dir/populations.csv" "$target_dir/populations.csv" || echo -e "$(tput setaf 1) \n - populations.csv not matching"
diff --strip-trailing-cr "$out_dir/sim_params.json" "$target_dir/test_params.json" || echo -e "$(tput setaf 1) \n - params not matching"
diff --strip-trailing-cr "$out_dir/clones.csv" "$target_dir/clones.csv" || echo -e "$(tput setaf 1) \n - clones.csv not matching"
diff --strip-trailing-cr "$out_dir/summary.csv" "$target_dir/summary.csv" -I "[0-9][0-9].[0-9]*" || echo -e "$(tput setaf 1) \n - summary.csv not matching" # Ignore time differences
echo "$(tput setaf 7)"
echo "------------------------------------------------------"
echo "- COMPARISON FINISHED --------------------------------"
echo "------------------------------------------------------"