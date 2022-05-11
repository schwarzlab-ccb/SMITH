using System.Globalization;
using System.Text.Json;
using SimChA.DataTypes;

namespace SimChA.IO;

public class FileIO
{
    private const string DOT_FILENAME = "parent_graph.dot";

    private const string SUBCLONES_FILENAME = "subclones.out";

    private const string POPULATIONS_DF_FILENAME = "populations.csv";
    private const string ADJACENCY_DF_FILENAME = "parent_tree.csv";
    private const string SIM_PARAMS_FILENAME = "sim_params.json";
    private const string CCF_FILENAME = "ccf.csv";
    private const string SUMMARY_FILENAME = "summary.csv";

    public FileIO(string rootFolder, bool isRepeated)
    {
        Timestamp = DateTime.Now.ToString("yy_MM_dd_HH_mm_ss");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        RootFolder = rootFolder;
        if (Directory.Exists(RootFolder))
        {
            foreach (var file in new DirectoryInfo(RootFolder).GetFiles())
            {
                file.Delete();
            }
        }
        else
        {
            Directory.CreateDirectory(RootFolder);
        }

        IsRepeated = isRepeated;

        if (IsRepeated)
        {
            ExperimentFolder = Path.Join(RootFolder, Timestamp);
            Directory.CreateDirectory(ExperimentFolder);
        }
        else
        {
            ExperimentFolder = RootFolder;
        }

        CreateSummary();
    }

    private string Timestamp { get; }
    private string RootFolder { get; }
    private string ExperimentFolder { get; }

    private bool IsRepeated { get; }

    public void WriteSubClones(IEnumerable<SubClone> subClones)
    {
        string outPath = Path.Combine(Path.GetFullPath(RootFolder), SUBCLONES_FILENAME);
        using var outputFile = new StreamWriter(outPath);

        foreach (var subClone in subClones)
        {
            outputFile.WriteLine(subClone);
        }
    }

    public void WriteParentTree(ParentTree tree)
    {
        string outPath = Path.Combine(Path.GetFullPath(RootFolder), DOT_FILENAME);
        using var outputFile = new StreamWriter(outPath);

        outputFile.WriteLine("Digraph SimChA {");
        foreach (var node in tree.Nodes)
        {
            double size = Math.Round(.25 * (1 + Math.Log(1 + node.Size)), 2);
            outputFile.WriteLine($"\t{node.Id} [label=\"{node.Id}:{node.Size}\", width={size}, height={size * .6}];");
        }

        foreach (var edge in tree.Edges)
        {
            outputFile.WriteLine($"\t{edge.SourceId} -> {edge.TargetId} [label=\"{edge.Distance}\"];");
        }

        outputFile.WriteLine("}");
    }

    public void WriteMullerDataFrames(IEnumerable<SubClone> subClones, ParentTree tree)
    {
        string popPath = Path.Combine(Path.GetFullPath(RootFolder), POPULATIONS_DF_FILENAME);
        string adjPath = Path.Combine(Path.GetFullPath(RootFolder), ADJACENCY_DF_FILENAME);

        using var popFile = new StreamWriter(popPath);
        popFile.WriteLine("Id,Step,Pop,Drivers");
        int lastGen = subClones.Max(sc => sc.LastGen);
        foreach (var subClone in subClones)
        {
            int start = subClone.FirstGen;
            // int end = subClone.LastGen;
            for (int gen = start; gen < lastGen; gen++)
            {
                long totalCells = subClone.AliveAtGen(gen);
                if (totalCells > 0)
                {
                    popFile.WriteLine($"{subClone.CloneId},{gen},{totalCells},{subClone.NumberDrivers}");
                }
            }
        }

        using var adjFile = new StreamWriter(adjPath);
        adjFile.WriteLine("ParentId,ChildId");

        if (tree.Edges.Any())
        {
            foreach (var edge in tree.Edges)
            {
                adjFile.WriteLine($"\t{edge.SourceId},{edge.TargetId}");
            }
        }
        else
        {
            adjFile.WriteLine($"\t-1,{subClones.First().CloneId}");
        }
    }

    public void WriteCCF(Dictionary<int, long> vaf, long totalSize)
    {
        string outPath = Path.Combine(Path.GetFullPath(RootFolder), CCF_FILENAME);
        using var outputFile = new StreamWriter(outPath);
        outputFile.WriteLine("id,pop,ccf");
        foreach ((int id, long pop) in vaf)
        {
            outputFile.WriteLine($"{id},{pop},{(float)pop / totalSize}");
        }
    }

    public void WriteSimParams(SimParams simParams)
    {
        string filePath = Path.Combine(Path.GetFullPath(ExperimentFolder), SIM_PARAMS_FILENAME);
        using var file = new StreamWriter(filePath);
        var options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(simParams, options);
        file.WriteLine(jsonString);
    }

    public static SimParams SimParamsFromFile(string filePath)
    {
        string fileFullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fileFullPath))
        {
            throw new Exception($"Configuration file {fileFullPath} does not exist");
        }

        try
        {
            string serializedJSON = File.ReadAllText(fileFullPath);
            var options = new JsonSerializerOptions { IncludeFields = true };
            var simParams = JsonSerializer.Deserialize<SimParams>(serializedJSON, options);
            if (simParams.Seed < 0)
            {
                simParams.Seed = new Random().Next();
            }

            return simParams;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to read simulation params from the file {fileFullPath}. Error {e.Message}");
        }
    }

    private void CreateSummary()
    {
        string filePath = Path.Combine(Path.GetFullPath(ExperimentFolder), SUMMARY_FILENAME);
        using var file = new StreamWriter(filePath);
        file.WriteLine(ResultSummary.Header());
    }

    public void AddToSummary(ResultSummary resultSummary)
    {
        string filePath = Path.Combine(Path.GetFullPath(ExperimentFolder), SUMMARY_FILENAME);
        using var file = new StreamWriter(filePath, true);
        file.WriteLine(resultSummary.ToString());
        file.Flush();
    }

    public void StoreCopy(int runId)
    {
        if (!IsRepeated)
        {
            return;
        }

        string copyFolder = Path.Join(ExperimentFolder, runId.ToString());
        Directory.CreateDirectory(copyFolder);

        foreach (var file in new DirectoryInfo(RootFolder).GetFiles())
        {
            file.CopyTo(Path.Join(copyFolder, file.Name));
        }
    }

    public void CopySummary()
    {
        if (!IsRepeated)
        {
            return;
        }

        File.Copy(
            Path.Combine(Path.GetFullPath(ExperimentFolder), SUMMARY_FILENAME),
            Path.Combine(Path.GetFullPath(RootFolder), SUMMARY_FILENAME));

        File.Copy(
            Path.Combine(Path.GetFullPath(ExperimentFolder), SIM_PARAMS_FILENAME),
            Path.Combine(Path.GetFullPath(RootFolder), SIM_PARAMS_FILENAME));
    }
}