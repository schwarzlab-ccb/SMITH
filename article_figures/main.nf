nextflow.enable.dsl=2

def onlySelection = params.containsKey('only') ? params.only.toString() : 'all'
def parallelism = (params.containsKey('max_forks') ? params.max_forks : Runtime.runtime.availableProcessors()) as int
def repoRoot = projectDir.parent.toString()
def articleFiguresDir = projectDir.toString()
def resultsDir = (params.containsKey('results_dir') ? params.results_dir : "${projectDir.parent}/out/results").toString()

def collectConfigRecords(String label, String cfgDirPath) {
    def cfgDir = new File(cfgDirPath)

    if (!cfgDir.isDirectory()) {
        throw new IllegalArgumentException("Config directory not found: ${cfgDirPath}")
    }

    def cfgs = cfgDir
        .listFiles()
        .findAll { it.isFile() && it.name.endsWith('_sim_params.json') }
        .sort { it.name }

    return cfgs.collect { cfg ->
        def runStub = cfg.name - '_sim_params.json'
        [label, runStub, cfg.absolutePath]
    }
}

if (!(onlySelection in ['all', 'fish', 'trajectories', 'grid'])) {
    throw new IllegalArgumentException("Invalid value for --only: ${onlySelection}")
}

if (parallelism < 1) {
    throw new IllegalArgumentException("--max_forks must be at least 1")
}

process RUN_SIMULATION {
    tag "${label}:${run_stub}"
    maxForks parallelism

    input:
    tuple val(label), val(run_stub), path(cfg)

    output:
    val(run_stub)

    script:
    """
    set -euo pipefail

    run_dotnet() {
        if command -v dotnet >/dev/null 2>&1; then
            local version
            version="\$(dotnet --version 2>/dev/null || true)"
            if [[ "\$version" =~ ^([0-9]+)\\. ]] && (( \${BASH_REMATCH[1]} >= 10 )); then
                dotnet "\$@"
                return
            fi
        fi

        if command -v conda >/dev/null 2>&1 && conda run -n smith dotnet --version >/dev/null 2>&1; then
            conda run -n smith dotnet "\$@"
            return
        fi

        echo "dotnet >= 10 is required to run simulations." >&2
        exit 1
    }

    out_dir="${resultsDir}/parameter_range_${run_stub}"
    echo "[start] ${cfg.getName()}"
    if [[ -f "\$out_dir/populations.csv" && -f "\$out_dir/parent_tree.csv" ]]; then
        echo "[skip] ${cfg.getName()} (existing outputs found)"
        exit 0
    fi

    mkdir -p "\$out_dir"
    echo "[run ] ${cfg.getName()}"
    run_dotnet run --project "${repoRoot}/SMITH.csproj" -- -C "${cfg}" -O "\$out_dir" -N
    """
}

process RUN_GRID_SIMULATION {
    tag "grid:g${conf_global}:l${conf_local}:r${rep_idx}"
    maxForks parallelism

    input:
    tuple val(conf_global), val(conf_local), val(rep_idx)

    output:
    tuple val(conf_global), val(conf_local), val(rep_idx)

    script:
    def runStub = String.format("%.3f_%.3f_%03d", conf_global as double, conf_local as double, rep_idx as int)
    def seed = "grid_${runStub}".hashCode() & 0x7FFFFFFF
    def outDir = "${resultsDir}/grid_search_${runStub}"
    def configJson = """{
  "Seed": ${seed},
  "StartMut": 1,
  "StartPop": 1,
  "Reps": 1,
  "MaxPop": 1048576000,
  "MaxSteps": 1000000,
  "MaxClones": -1,
  "MinPop": 1000,
  "MaxTries": 10000,
  "Turnover": 0.01,
  "MutationProb": 2E-05,
  "DriverProb": 1,
  "FitnessMean": 0.1,
  "ConfGlobal": ${conf_global},
  "ConfLocal": ${conf_local},
  "FitnessAcc": "Add",
  "FitnessDist": "Exponential",
  "FitnessEffect": "Birth",
  "Checkpoints": true,
  "CutOff": 1E-09,
  "CloneSample": -1,
  "CalcFish": false,
  "FishFrac": 0.001
}"""
    """
    set -euo pipefail

    run_dotnet() {
        if command -v dotnet >/dev/null 2>&1; then
            local version
            version="\$(dotnet --version 2>/dev/null || true)"
            if [[ "\$version" =~ ^([0-9]+)\\. ]] && (( \${BASH_REMATCH[1]} >= 10 )); then
                dotnet "\$@"
                return
            fi
        fi

        if command -v conda >/dev/null 2>&1 && conda run -n smith dotnet --version >/dev/null 2>&1; then
            conda run -n smith dotnet "\$@"
            return
        fi

        echo "dotnet >= 10 is required to run simulations." >&2
        exit 1
    }

    out_dir="${outDir}"
    echo "[start] grid g=${conf_global} l=${conf_local} r=${rep_idx}"
    if [[ -f "\$out_dir/populations.csv" ]]; then
        echo "[skip] (existing outputs found)"
        exit 0
    fi

    mkdir -p "\$out_dir"
    cat > config.json << 'GRID_CONFIG_EOF'
${configJson}
GRID_CONFIG_EOF

    echo "[run ] grid g=${conf_global} l=${conf_local} r=${rep_idx}"
    run_dotnet run --project "${repoRoot}/SMITH.csproj" -- -C config.json -O "\$out_dir" -N
    """
}

workflow REPRESENTATIVE_RUNS {
    def configRecords = []

    if (onlySelection in ['all', 'fish']) {
        configRecords.addAll(collectConfigRecords('fish', "${articleFiguresDir}/data/fish_plot_configs"))
    }

    if (onlySelection in ['all', 'trajectories']) {
        configRecords.addAll(collectConfigRecords('trajectory', "${articleFiguresDir}/data/trajectories_configs"))
    }

    if (configRecords.isEmpty()) {
        throw new IllegalStateException('No config files found for the selected dataset(s).')
    }

    log.info "Queueing ${configRecords.size()} simulation tasks with maxForks=${parallelism}."

    def completedRuns = RUN_SIMULATION(
        Channel
            .fromList(configRecords)
            .map { label, runStub, cfgPath -> tuple(label, runStub, file(cfgPath)) }
    ).collect()

    completedRuns.view { runs -> "Completed simulation stage for ${runs.size()} runs." }
}

workflow PARAMETER_GRID_SEARCH {
    // Section 2.9: all combinations of hconf and hlocal from the paper, 100 replicates each
    def confValues = [0.0, 0.125, 0.25, 0.5, 1.0, 2.0]
    def nReplicates = 100

    def combinations = []
    confValues.each { g ->
        confValues.each { l ->
            (1..nReplicates).each { r ->
                combinations << [g, l, r]
            }
        }
    }

    log.info "Queueing ${combinations.size()} grid search tasks (${confValues.size()}×${confValues.size()} confinement grid × ${nReplicates} replicates) with maxForks=${parallelism}."

    def completedRuns = RUN_GRID_SIMULATION(
        Channel.fromList(combinations)
    ).collect()

    completedRuns.view { runs -> "Completed grid search stage for ${runs.size()} runs." }
}

workflow {
    if (onlySelection in ['all', 'fish', 'trajectories']) {
        REPRESENTATIVE_RUNS()
    }

    if (onlySelection == 'grid') {
        PARAMETER_GRID_SEARCH()
    }
}
