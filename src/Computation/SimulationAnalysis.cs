using SMITH.DataTypes;
using SMITH.Simulation;

namespace SMITH.Computation;

public static class SimulationAnalysis
{
    public static (ResultSummary Summary, ListTree LcaTree, List<Clone> Sample, Dictionary<int, long> Ccf)
        AnalyzeCheckpoint(int repeatId, int tryNo, int checkpointId, Simulator simulator, SimParams simParams, PopState popState,
            TimeSpan elapsed)
    {
        double cutOff = popState.Alive * simParams.CutOff;
        var aboveCutOff = simulator.Clones.Where(sc => sc.AliveCount >= cutOff).ToList();
        var cloneSample = (simParams.CloneSample > 0 && simParams.CloneSample < aboveCutOff.Count
            ? aboveCutOff.OrderByDescending(c => c.AliveCount).Take(simParams.CloneSample)
            : aboveCutOff).ToList();

        var lcaTreeList = TreeBuilder.BuildLCAT(simulator.Clones, cloneSample);
        var treeNodes = lcaTreeList.Nodes.Select(n => n.Id).ToHashSet();
        var sample = simulator.Clones.Where(sc => treeNodes.Contains(sc.CloneId)).ToList();
        var ccCount = TreeAnalysis.ComputeCCF(lcaTreeList);

        string time = elapsed.ToString();
        var result = new ResultSummary(repeatId, tryNo, checkpointId, simulator.StepNo, time,
            lcaTreeList, cloneSample, simulator.Clones, popState, ccCount);

        return (result, lcaTreeList, sample, ccCount);
    }
}

