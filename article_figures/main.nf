nextflow.enable.dsl=2

def onlySelection = params.containsKey('only') ? params.only.toString() : 'all'
def forceRuns = (params.containsKey('force') ? params.force : false).toString().toBoolean()
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

if (!(onlySelection in ['all', 'fish', 'trajectories'])) {
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
    if [[ "${forceRuns}" != "true" && -f "\$out_dir/populations.csv" && -f "\$out_dir/parent_tree.csv" ]]; then
        echo "[skip] ${cfg.getName()} (existing outputs found)"
        exit 0
    fi

    mkdir -p "\$out_dir"
    echo "[run ] ${cfg.getName()}"
    run_dotnet run --project "${repoRoot}/SMITH.csproj" -- -C "${cfg}" -O "\$out_dir" -N
    """
}

workflow {
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