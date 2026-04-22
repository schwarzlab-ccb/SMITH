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

        try
        {
            var files = new FileIO(outDir, isRepeated: false);
            files.WriteSimParams(simParams);

            var runner = new SimulationRunner(simParams, files, new Random(simParams.Seed), logNewline: true, bifrucating: false);
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

            string expectedSummary = TestHelper.NormalizeSummaryTimes(
                File.ReadAllText(Path.Combine(TestHelper.FixtureFolder, "summary.csv")));
            string actualSummary = TestHelper.NormalizeSummaryTimes(
                File.ReadAllText(Path.Combine(outDir, "summary.csv")));
            Assert.Equal(expectedSummary, actualSummary);
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }
        }
    }

    [Fact]
    public void RunAll_WithBifrucatingFlag_KeepsCloneTree_AndAddsBinTree()
    {
        var simParams = TestHelper.LoadFixtureParams();
        string outDir = TestHelper.CreateTempDirectory();

        try
        {
            var files = new FileIO(outDir, isRepeated: false);
            files.WriteSimParams(simParams);

            var runner = new SimulationRunner(simParams, files, new Random(simParams.Seed), logNewline: true, bifrucating: true);
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

            Assert.True(File.Exists(Path.Combine(outDir, "bin_tree.dot")));
            Assert.True(File.Exists(Path.Combine(outDir, "bin_tree.new")));

            string binDot = File.ReadAllText(Path.Combine(outDir, "bin_tree.dot"));
            string binNewick = File.ReadAllText(Path.Combine(outDir, "bin_tree.new"));
            Assert.NotEmpty(binDot);
            Assert.NotEmpty(binNewick);

            Assert.Matches("label=\"-?\\d+-\\d+\"", binDot);
            Assert.Matches("-?\\d+-\\d+;", binNewick);
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }
        }
    }
}


