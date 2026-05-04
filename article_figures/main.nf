nextflow.enable.dsl=2

def repoRoot = projectDir.parent.toString()
def articleFiguresDir = projectDir.toString()
def resultsDir = (params.containsKey('results_dir') ? params.results_dir : "${projectDir.parent}/out/results").toString()

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

process RUN_SIMULATION {
    input:
    val(config_records)

    script:
    def commands = config_records.collect { runStub, cfgPath ->
        def cfgName = new File(cfgPath).name
        """
    out_dir=\"${resultsDir}/parameter_range_${runStub}\"
    echo \"[start] ${cfgName}\"
    if [[ -f \"${'$'}out_dir/populations.csv\" && -f \"${'$'}out_dir/parent_tree.csv\" ]]; then
        echo \"[skip] ${cfgName} (existing outputs found)\"
    else
        mkdir -p \"${'$'}out_dir\"
        echo \"[run ] ${cfgName}\"
        \"${repoRoot}/bin/Release/net10.0/publish/SMITH\" -C \"${cfgPath}\" -O \"${'$'}out_dir\" -N
    fi
        """.stripIndent().trim()
    }.join('\n\n')
    """
    set -euo pipefail

    ${commands}
    """
}

process RUN_GRID_SIMULATION {
    input:
    val(combinations)

    script:
    def commands = combinations.collect { confGlobal, confLocal, repIdx, seed ->
        def runStub = String.format("%.3f_%.3f_%03d", confGlobal as double, confLocal as double, repIdx as int)
        def outDir = "${resultsDir}/grid_search_${runStub}"
        """
    out_dir=\"${outDir}\"
    echo \"[start] grid g=${confGlobal} l=${confLocal} r=${repIdx}\"
    if [[ -f \"${'$'}out_dir/populations.csv\" ]]; then
        echo \"[skip] (existing outputs found)\"
    else
        mkdir -p \"${'$'}out_dir\"
        jq --argjson seed ${seed} \\
           --argjson confGlobal ${confGlobal} \\
           --argjson confLocal ${confLocal} \\
           '. + {\"Seed\": ${'$'}seed, \"ConfGlobal\": ${'$'}confGlobal, \"ConfLocal\": ${'$'}confLocal}' \\
           \"${articleFiguresDir}/article_config.json\" > \"${'$'}out_dir/config.json\"
        echo \"[run ] grid g=${confGlobal} l=${confLocal} r=${repIdx}\"
        \"${repoRoot}/bin/Release/net10.0/publish/SMITH\" -C \"${'$'}out_dir/config.json\" -O \"${'$'}out_dir\" -N
    fi
        """.stripIndent().trim()
    }.join('\n\n')
    """
    set -euo pipefail

    ${commands}
    """
}

workflow REPRESENTATIVE_RUNS {
    def configRecords = collectConfigRecords([
        "${articleFiguresDir}/data/fish_plot_configs",
        "${articleFiguresDir}/data/trajectories_configs"
    ])

    if (configRecords.isEmpty()) {
        throw new IllegalStateException('No config files found.')
    }

    log.info "Queueing ${configRecords.size()} simulation runs in one batch job."

    RUN_SIMULATION(
        Channel.value(configRecords)
    )
}

workflow PARAMETER_GRID_SEARCH {
    // Section 2.9: all combinations of hconf and hlocal from the paper, 100 replicates each
    def confValues = [0.0, 0.125, 0.25, 0.5, 1.0, 2.0]
    def nReplicates = 100

    def combinations = []
    confValues.each { g ->
        confValues.each { l ->
            (1..nReplicates).each { r ->
                def runStub = String.format("%.3f_%.3f_%03d", g as double, l as double, r as int)
                def seed = "grid_${runStub}".hashCode() & 0x7FFFFFFF
                combinations << [g, l, r, seed]
            }
        }
    }

    log.info "Queueing ${combinations.size()} grid search tasks in one batch job."

    RUN_GRID_SIMULATION(
        Channel.value(combinations)
    )
}

workflow {
    REPRESENTATIVE_RUNS()
    PARAMETER_GRID_SEARCH()
}
