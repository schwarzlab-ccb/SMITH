nextflow.enable.dsl=2

def repoRoot = projectDir.parent.toString()
def articleFiguresDir = projectDir.toString()
def resultsDir = (params.containsKey('results_dir') ? params.results_dir : "${projectDir.parent}/out/results").toString()
def runMode = (params.containsKey('only') ? params.only : 'all').toString()
def validRunModes = ['all', 'fish', 'trajectories', 'grid']

if (!validRunModes.contains(runMode)) {
    throw new IllegalArgumentException(
        "Invalid --only value '${runMode}'. Expected one of: ${validRunModes.join(', ')}")
}

def collectConfigRecords(List<String> cfgDirPaths) {
    def seen = new LinkedHashSet<String>()
    def records = []

    cfgDirPaths.each { cfgDirPath ->
        def cfgDir = new File(cfgDirPath)
        if (!cfgDir.isDirectory()) {
            throw new IllegalArgumentException("Config directory not found: ${cfgDirPath}")
        }
        cfgDir.listFiles()
            .findAll { it.isFile() && it.name.endsWith('_sim_params.json') }
            .sort { it.name }
            .each { cfg ->
                def runStub = cfg.name - '_sim_params.json'
                if (seen.add(runStub)) {
                    records << [runStub, cfg.absolutePath]
                }
            }
    }
    return records
}

def collectGridSearchRecords(List<Double> confValues, int nReplicates) {
    def records = []

    confValues.each { g ->
        confValues.each { l ->
            (1..nReplicates).each { r ->
                def runStub = String.format("%.3f_%.3f_%03d", g as double, l as double, r as int)
                def seed = "grid_${runStub}".hashCode() & 0x7FFFFFFF
                records << [g, l, r, seed]
            }
        }
    }

    return records
}

process RUN_SIMULATION {
    tag { run_stub }

    input:
    tuple val(run_stub), path(cfg)

    script:
    """
    set -euo pipefail

    out_dir="${resultsDir}/parameter_range_${run_stub}"
    calc_fish=\$(jq -r '.CalcFish // false' "${cfg}")
    echo "[start] ${cfg.getName()}"
    if [[ -s "\$out_dir/summary.csv" \
          && -s "\$out_dir/sim_params.json" \
          && -s "\$out_dir/clone_tree.new" \
          && -s "\$out_dir/clones.csv" \
          && ( "\$calc_fish" != "true" \
               || ( -s "\$out_dir/populations.csv" && -s "\$out_dir/parent_tree.csv" ) ) ]]; then
        echo "[skip] ${cfg.getName()} (existing outputs found)"
        exit 0
    fi

    mkdir -p "\$out_dir"
    echo "[run ] ${cfg.getName()}"
    "${repoRoot}/bin/Release/net10.0/SMITH" -C "${cfg}" -O "\$out_dir" -N

    if [[ ! -s "\$out_dir/summary.csv" \
          || ! -s "\$out_dir/sim_params.json" \
          || ! -s "\$out_dir/clone_tree.new" \
          || ! -s "\$out_dir/clones.csv" \
          || ( "\$calc_fish" == "true" \
               && ( ! -s "\$out_dir/populations.csv" || ! -s "\$out_dir/parent_tree.csv" ) ) ]]; then
        echo "[error] ${cfg.getName()} completed without all expected outputs" >&2
        exit 1
    fi
    """
}

process RUN_GRID_SIMULATION {
    tag { String.format("%.3f_%.3f_%03d", conf_global as double, conf_local as double, rep_idx as int) }

    input:
    tuple val(conf_global), val(conf_local), val(rep_idx), val(seed)

    script:
    def runStub = String.format("%.3f_%.3f_%03d", conf_global as double, conf_local as double, rep_idx as int)
    def outDir = "${resultsDir}/grid_search_${runStub}"
    """
    set -euo pipefail

    out_dir="${outDir}"
    echo "[start] grid g=${conf_global} l=${conf_local} r=${rep_idx}"
    if [[ -s "\$out_dir/summary.csv" \
          && -s "\$out_dir/sim_params.json" \
          && -s "\$out_dir/clone_tree.new" \
          && -s "\$out_dir/clones.csv" ]]; then
        echo "[skip] (existing outputs found)"
        exit 0
    fi

    mkdir -p "\$out_dir"
    jq --argjson seed ${seed} \
       --argjson confGlobal ${conf_global} \
       --argjson confLocal ${conf_local} \
       '. + {"Seed": \$seed, "ConfGlobal": \$confGlobal, "ConfLocal": \$confLocal}' \
       "${articleFiguresDir}/article_config.json" > grid_config.json
    echo "[run ] grid g=${conf_global} l=${conf_local} r=${rep_idx}"
    "${repoRoot}/bin/Release/net10.0/SMITH" -C grid_config.json -O "\$out_dir" -N

    if [[ ! -s "\$out_dir/summary.csv" \
          || ! -s "\$out_dir/sim_params.json" \
          || ! -s "\$out_dir/clone_tree.new" \
          || ! -s "\$out_dir/clones.csv" ]]; then
        echo "[error] grid g=${conf_global} l=${conf_local} r=${rep_idx} completed without all expected outputs" >&2
        exit 1
    fi
    """
}

workflow REPRESENTATIVE_RUNS {
    def configDirs = []
    if (runMode in ['all', 'fish']) {
        configDirs << "${articleFiguresDir}/data/fish_plot_configs"
    }
    if (runMode in ['all', 'trajectories']) {
        configDirs << "${articleFiguresDir}/data/trajectories_configs"
    }
    def configRecords = collectConfigRecords(configDirs)

    if (configRecords.isEmpty()) {
        throw new IllegalStateException('No config files found.')
    }

    log.info "Queueing ${configRecords.size()} representative simulation runs."

    RUN_SIMULATION(
        Channel
            .fromList(configRecords)
            .map { runStub, cfgPath -> tuple(runStub, file(cfgPath)) }
    )
}

workflow PARAMETER_GRID_SEARCH {
    // Section 2.9: all combinations of hconf and hlocal from the paper, 100 replicates each
    def confValues = [0.0, 0.125, 0.25, 0.5, 1.0, 2.0]
    def nReplicates = 100

    def combinations = collectGridSearchRecords(confValues, nReplicates)

    log.info "Queueing ${combinations.size()} grid search tasks."

    RUN_GRID_SIMULATION(
        Channel.fromList(combinations)
    )
}

workflow {
    if (runMode in ['all', 'fish', 'trajectories']) {
        REPRESENTATIVE_RUNS()
    }
    if (runMode in ['all', 'grid']) {
        PARAMETER_GRID_SEARCH()
    }
}
