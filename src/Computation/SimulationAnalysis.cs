using SMITH.DataTypes;
using SMITH.Simulation;

namespace SMITH.Computation;

public static class SimulationAnalysis
{
    public static (ResultSummary Summary, ListTree LcaTree, List<Clone> Sample, Dictionary<int, long> Ccf)
        AnalyzeCheckpoint(int repeatId, int tryNo, int checkpointId, int populationId,
            int stepNo, List<Clone> population, SimParams simParams, PopState popState, TimeSpan elapsed)
    {
        double cutOff = popState.Alive * simParams.CutOff;
        var aboveCutOff = population.Where(sc => sc.AliveCount >= cutOff).ToList();
        var cloneSample = (simParams.CloneSample > 0 && simParams.CloneSample < aboveCutOff.Count
            ? aboveCutOff.OrderByDescending(c => c.AliveCount).Take(simParams.CloneSample)
            : aboveCutOff).ToList();

        var lcaTreeList = TreeBuilder.BuildLCAT(population, cloneSample);
        var treeNodes = lcaTreeList.Nodes.Select(n => n.Id).ToHashSet();
        var sample = population.Where(sc => treeNodes.Contains(sc.CloneId)).ToList();
        var ccCount = TreeAnalysis.ComputeCCF(lcaTreeList);

        string time = elapsed.ToString();
        var result = new ResultSummary(populationId, repeatId, tryNo, checkpointId, stepNo, time,
            lcaTreeList, cloneSample, population, popState, ccCount);

        return (result, lcaTreeList, sample, ccCount);
    }
}
