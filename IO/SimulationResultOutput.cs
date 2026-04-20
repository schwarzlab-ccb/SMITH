using SMITH.Computation;
using SMITH.DataTypes;
using SMITH.Simulation;

namespace SMITH.IO;

public static class SimulationResultOutput
{
    public static void WriteFinishedOutputs(FileIO files, ResultSummary result, ListTree lcaTreeList, List<Clone> sample,
        Dictionary<int, long> ccCount, long aliveCount, Simulator simulator, SimParams simParams, List<PopState> popStates,
        int repeatId, int tryNo)
    {
        var parentMap = lcaTreeList.Edges.ToDictionary(e => e.TargetId, e => (e.SourceId, e.Distance));
        files.WriteClones(sample, ccCount, aliveCount, parentMap);
        files.WriteDotTree(lcaTreeList);

        var tree = TreeBuilder.ListToTree(lcaTreeList);
        files.WriteNewickTree(tree);

        var (mullerPops, mullerTree) = State.GetMullerData(simulator, simParams, popStates);
        if (mullerPops.Any())
        {
            files.WriteMullerDataFrames(mullerPops, mullerTree);
        }

        files.StoreCopy(repeatId);
        Console.WriteLine($"Sim: {repeatId + 1}.{tryNo}/{simParams.Reps} result:".PadRight(160));
        Console.WriteLine(result.ToText());
        GC.Collect();
    }
}

