#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
DATA_DIR="$REPO_ROOT/article_figures/data"
FISH_CFG_DIR="$DATA_DIR/fish_plot_configs"
TRAJ_CFG_DIR="$DATA_DIR/trajectories_configs"

# Keep generated raw simulation runs in the default project output folder.
RESULTS_DIR="$REPO_ROOT/out/results"
ONLY="all"
FORCE=0
SKIP_SIM=0

usage() {
    cat <<EOF
Usage: $(basename "$0") [options]

Generate missing article figure data artifacts used by create_figures.ipynb:
  - article_figures/data/fish_plot_data.pkl
  - article_figures/data/trajectories.pkl

Options:
    --results-dir <path>   Output folder for generated raw simulation runs.
                                                 Default: $REPO_ROOT/out/results
  --only <all|fish|trajectories>
                         Which dataset to build. Default: all
  --force                Re-run simulations even if output files exist.
  --skip-sim             Skip simulations and only rebuild pickle files from existing runs.
  -h, --help             Show this help.

Example:
  bash article_figures/scripts/run_streck_repro.sh --only fish
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --results-dir)
            RESULTS_DIR="$2"
            shift 2
            ;;
        --only)
            ONLY="$2"
            shift 2
            ;;
        --force)
            FORCE=1
            shift
            ;;
        --skip-sim)
            SKIP_SIM=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage
            exit 1
            ;;
    esac
done

if [[ "$ONLY" != "all" && "$ONLY" != "fish" && "$ONLY" != "trajectories" ]]; then
    echo "Invalid value for --only: $ONLY" >&2
    exit 1
fi

if [[ ! -d "$FISH_CFG_DIR" || ! -d "$TRAJ_CFG_DIR" ]]; then
    echo "Config directories not found under $DATA_DIR" >&2
    exit 1
fi

if ! command -v python >/dev/null 2>&1; then
    echo "python is required but was not found in PATH." >&2
    exit 1
fi

if [[ "$SKIP_SIM" -eq 0 ]] && ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet is required to run simulations but was not found in PATH." >&2
    exit 1
fi

mkdir -p "$RESULTS_DIR"

run_config_dir() {
    local cfg_dir="$1"
    local label="$2"

    mapfile -t cfgs < <(find "$cfg_dir" -maxdepth 1 -type f -name '*_sim_params.json' | sort)
    if [[ "${#cfgs[@]}" -eq 0 ]]; then
        echo "No config files found in $cfg_dir"
        return
    fi

    echo "Running $label simulations (${#cfgs[@]} configs)..."

    for cfg in "${cfgs[@]}"; do
        local name
        name="$(basename "$cfg")"
        local run_stub
        run_stub="${name%_sim_params.json}"
        local out_dir="$RESULTS_DIR/parameter_range_${run_stub}"

        if [[ "$FORCE" -eq 0 && -f "$out_dir/populations.csv" && -f "$out_dir/parent_tree.csv" ]]; then
            echo "[skip] $name (existing outputs found)"
            continue
        fi

        mkdir -p "$out_dir"
        echo "[run ] $name"
        dotnet run --project "$REPO_ROOT/SMITH.csproj" -- -C "$cfg" -O "$out_dir" -N
    done
}

if [[ "$SKIP_SIM" -eq 0 ]]; then
    if [[ "$ONLY" == "all" || "$ONLY" == "fish" ]]; then
        run_config_dir "$FISH_CFG_DIR" "fish"
    fi
    if [[ "$ONLY" == "all" || "$ONLY" == "trajectories" ]]; then
        run_config_dir "$TRAJ_CFG_DIR" "trajectory"
    fi
else
    echo "Skipping simulations (--skip-sim)."
fi

export STRECK_RESULTS_DIR="$RESULTS_DIR"

if [[ "$ONLY" != "all" ]]; then
    echo "Skipping pickle generation because --only=$ONLY was selected."
    echo "Run with --only all (default) to rebuild article_figures/data/*.pkl."
    echo "Done."
    exit 0
fi

(
    cd "$REPO_ROOT/article_figures"
    python "scripts/create_plotting_data_from_raw.py"
)

echo "Done."
