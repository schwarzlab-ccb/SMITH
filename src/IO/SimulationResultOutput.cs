using SMITH.Computation;
using SMITH.DataTypes;
using SMITH.Simulation;

namespace SMITH.IO;

public static class SimulationResultOutput
{
    public static void WriteFinishedOutputs(FileIO files, ResultSummary result, ListTree lcaTreeList, List<Clone> sample,
        Dictionary<int, long> ccCount, long aliveCount, IEnumerable<Clone> primaryClones, SimParams simParams,
        List<PopState> popStates, int repeatId, int tryNo, bool bifrucating, int popId = 0)
    {
        var parentMap = lcaTreeList.Edges.ToDictionary(e => e.TargetId, e => (e.SourceId, e.Distance));
        files.WriteClones(sample, ccCount, aliveCount, parentMap, popId);

        var tree = TreeBuilder.ListToTree(lcaTreeList);
        files.WriteDotTree(lcaTreeList, popId);
        files.WriteNewickTree(tree, popId);

        if (bifrucating)
        {
            var firstGen = TreeBuilder.CountFirstGent(sample);
            var binTree = TreeBuilder.CloneTree(tree);
            TreeBuilder.ConvertToBifrucatingNodes(firstGen, binTree);
            files.WriteBinDotTree(binTree, popId);
            files.WriteBinNewickTree(binTree, popId);
        }

        // Muller/fish data only for the primary population
        if (popId == 0)
        {
            var (mullerPops, mullerTree) = State.GetMullerData(primaryClones, simParams, popStates);
            if (mullerPops.Any())
            {
                files.WriteMullerDataFrames(mullerPops, mullerTree);
            }

            Console.WriteLine($"Sim: {repeatId + 1}.{tryNo}/{simParams.Reps} result:".PadRight(160));
            Console.WriteLine(result.ToText());
        }

        GC.Collect();
    }
}
