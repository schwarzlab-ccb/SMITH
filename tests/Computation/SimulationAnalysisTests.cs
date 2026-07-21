using SMITH.Computation;
using SMITH.DataTypes;
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
        var popState = CellSampling.PopState(simulator.Clones);

        // Step until we have enough clones to test sampling behavior.
        while (simulator.Clones.Count < 8 && simulator.StepNo < 2000)
        {
            simulator.Step();
            popState = CellSampling.PopState(simulator.Clones);
            if (State.GetCompState(popState, simulator, simParams) != ComputeState.Running)
            {
                break;
            }
        }

        var (summary, lcaTree, sample, ccf) = SimulationAnalysis.AnalyzeCheckpoint(
            repeatId: 0,
            tryNo: 0,
            checkpointId: 0,
            simulator: simulator,
            simParams: simParams,
            popState: popState,
            elapsed: TimeSpan.Zero);

        int expectedSelected = Math.Min(simParams.CloneSample, simulator.Clones.Count);
        Assert.Equal(expectedSelected, summary.SubcloneSelect);

        var treeNodeIds = lcaTree.Nodes.Select(n => n.Id).ToHashSet();
        Assert.All(sample, clone => Assert.Contains(clone.CloneId, treeNodeIds));

        Assert.Equal(lcaTree.Nodes.Count, ccf.Count);
        Assert.All(lcaTree.Nodes, node => Assert.True(ccf.ContainsKey(node.Id), $"Missing CCF entry for node {node.Id}"));
    }

    [Fact]
    public void AnalyzeCheckpoint_WithNoCloneAboveCutoff_ReturnsFiniteZeroMetrics()
    {
        var simParams = TestHelper.LoadFixtureParams();
        simParams.CutOff = 0.6;

        var simulator = new Simulator(simParams, new Random(simParams.Seed));
        simulator.Clones.Clear();
        simulator.Clones.Add(new Clone(0, -1, 0, 1, 0, 0, 50));
        simulator.Clones.Add(new Clone(1, 0, 0, 1, 1, 1, 50));
        var popState = CellSampling.PopState(simulator.Clones);

        var (summary, lcaTree, sample, ccf) = SimulationAnalysis.AnalyzeCheckpoint(
            0, 0, 0, simulator, simParams, popState, TimeSpan.Zero);

        Assert.Equal(0, summary.SubcloneSelect);
        Assert.Equal(0, summary.ClonalDiversity);
        Assert.Equal(0, summary.MeanDriversPerCell);
        Assert.Equal(0, summary.TreeBalance);
        Assert.Empty(lcaTree.Nodes);
        Assert.Empty(sample);
        Assert.Empty(ccf);
    }
}
