using SMITH.DataTypes;
using SMITH.IO;
using Xunit;

namespace SMITH.Tests.TestSupport;

internal static class TestHelper
{
    internal static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    internal static string FixtureFolder => Path.Combine(RepoRoot, "tests", "targets");

    internal static SimParams LoadFixtureParams()
    {
        return FileIO.SimParamsFromFile(Path.Combine(FixtureFolder, "test_params.json"));
    }

    internal static string CreateTempDirectory()
    {
        string path = Path.Combine(RepoRoot, "tests", "out");
        Directory.CreateDirectory(path);
        return path;
    }

    internal static void AssertTextFileEqual(string expectedPath, string actualPath)
    {
        Assert.True(File.Exists(expectedPath), $"Expected fixture file does not exist: {expectedPath}");
        Assert.True(File.Exists(actualPath), $"Expected output file does not exist: {actualPath}");

        string expected = File.ReadAllText(expectedPath).Replace("\r\n", "\n").TrimEnd();
        string actual = File.ReadAllText(actualPath).Replace("\r\n", "\n").TrimEnd();
        Assert.Equal(expected, actual);
    }

    internal static void AssertCsvShapeEqual(string expectedPath, string actualPath)
    {
        Assert.True(File.Exists(expectedPath), $"Expected fixture file does not exist: {expectedPath}");
        Assert.True(File.Exists(actualPath), $"Expected output file does not exist: {actualPath}");

        string[] expectedRows = File.ReadAllText(expectedPath)
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string[] actualRows = File.ReadAllText(actualPath)
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(expectedRows.Length, actualRows.Length);

        for (int i = 0; i < expectedRows.Length; i++)
        {
            int expectedColumns = expectedRows[i].Split(',').Length;
            int actualColumns = actualRows[i].Split(',').Length;
            Assert.Equal(expectedColumns, actualColumns);
        }
    }
}


