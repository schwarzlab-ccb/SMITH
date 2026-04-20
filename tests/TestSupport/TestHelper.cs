using SMITH.DataTypes;
using SMITH.IO;
using Xunit;

namespace SMITH.Tests.TestSupport;

internal static class TestHelper
{
    internal static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    internal static string FixtureFolder => Path.Combine(RepoRoot, "test");

    internal static SimParams LoadFixtureParams()
    {
        return FileIO.SimParamsFromFile(Path.Combine(FixtureFolder, "test_params.json"));
    }

    internal static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "smith-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    internal static void AssertTextFileEqual(string expectedPath, string actualPath)
    {
        Assert.True(File.Exists(expectedPath), $"Expected fixture file does not exist: {expectedPath}");
        Assert.True(File.Exists(actualPath), $"Expected output file does not exist: {actualPath}");

        string expected = File.ReadAllText(expectedPath).Replace("\r\n", "\n");
        string actual = File.ReadAllText(actualPath).Replace("\r\n", "\n");
        Assert.Equal(expected, actual);
    }

    internal static string NormalizeSummaryTimes(string summaryContent)
    {
        var lines = summaryContent.Replace("\r\n", "\n").Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var columns = lines[i].Split(',');
            if (columns.Length > 3)
            {
                columns[3] = "<TIME>";
                lines[i] = string.Join(',', columns);
            }
        }

        return string.Join('\n', lines).TrimEnd();
    }
}


