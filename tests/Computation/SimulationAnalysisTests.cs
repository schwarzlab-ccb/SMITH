using SMITH.Computation;
using SMITH.Simulation;
using SMITH.Tests.TestSupport;
using Xunit;

namespace SMITH.Tests.Computation;

public class SimulationAnalysisTests
{
    [Fact]
    public void AnalyzeCheckpoint_HonorsCloneSample_AndBuildsConsistentSample()
    {
        var simParams = TestHelper.LoadFixtureParams();
        simParams.CutOff = 0.0;
        simParams.CloneSample = 3;

        var simulator = new Simulator(simParams, new Random(simParams.Seed));
        var primaryPop = simulator.Populations[0];
        var popState = CellSampling.PopState(primaryPop);

        // Step until we have enough clones to test sampling behavior.
        while (primaryPop.Count < 8 && simulator.StepNo < 2000)
        {
            simulator.Step();
            popState = CellSampling.PopState(primaryPop);
            if (State.GetCompState(popState, simulator, simParams) != ComputeState.Running)
            {
                break;
            }
        }

        var (summary, lcaTree, sample, ccf) = SimulationAnalysis.AnalyzeCheckpoint(
            repeatId: 0,
            tryNo: 0,
            checkpointId: 0,
            populationId: 0,
            stepNo: simulator.StepNo,
            population: primaryPop,
            simParams: simParams,
            popState: popState,
            elapsed: TimeSpan.Zero);

        int expectedSelected = Math.Min(simParams.CloneSample, primaryPop.Count);
        Assert.Equal(expectedSelected, summary.SubcloneSelect);

        var treeNodeIds = lcaTree.Nodes.Select(n => n.Id).ToHashSet();
        Assert.All(sample, clone => Assert.Contains(clone.CloneId, treeNodeIds));

        Assert.Equal(lcaTree.Nodes.Count, ccf.Count);
        Assert.All(lcaTree.Nodes, node => Assert.True(ccf.ContainsKey(node.Id), $"Missing CCF entry for node {node.Id}"));
    }
}
