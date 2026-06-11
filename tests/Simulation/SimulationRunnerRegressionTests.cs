using SMITH.IO;
using SMITH.Simulation;
using SMITH.Tests.TestSupport;
using Xunit;

namespace SMITH.Tests.Simulation;

public class SimulationRunnerRegressionTests
{
    [Fact]
    public void RunAll_WithFixtureParams_MatchesReferenceOutputs()
    {
        var simParams = TestHelper.LoadFixtureParams();
        string outDir = TestHelper.CreateTempDirectory();
        
        var files = new FileIO(outDir, isRepeated: false);
        files.WriteSimParams(simParams);

        var rnd = new Random(simParams.Seed);
        var runner = new SimulationRunner(simParams, files, rnd, logNewline: true);
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        try
        {
            runner.RunAll();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        TestHelper.AssertTextFileEqual(
            Path.Combine(TestHelper.FixtureFolder, "clone_tree.dot"),
            Path.Combine(outDir, "clone_tree.dot"));
        TestHelper.AssertTextFileEqual(
            Path.Combine(TestHelper.FixtureFolder, "clone_tree.new"),
            Path.Combine(outDir, "clone_tree.new"));
        TestHelper.AssertTextFileEqual(
            Path.Combine(TestHelper.FixtureFolder, "parent_tree.csv"),
            Path.Combine(outDir, "parent_tree.csv"));
        TestHelper.AssertTextFileEqual(
            Path.Combine(TestHelper.FixtureFolder, "populations.csv"),
            Path.Combine(outDir, "populations.csv"));
        TestHelper.AssertTextFileEqual(
            Path.Combine(TestHelper.FixtureFolder, "clones.csv"),
            Path.Combine(outDir, "clones.csv"));
        TestHelper.AssertCsvShapeEqual(
            Path.Combine(TestHelper.FixtureFolder, "summary.csv"),
            Path.Combine(outDir, "summary.csv"));
    }
}


