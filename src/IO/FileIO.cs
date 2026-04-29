using System.Globalization;
using System.Text.Json;
using SMITH.DataTypes;

namespace SMITH.IO;

public class FileIO
{
    private static string PopSuffix(int popId) => popId == 0 ? "" : $"_{popId}";

    private static string SUBCLONES_FILENAME(int popId) => $"clones{PopSuffix(popId)}.csv";
    private static string DOT_TREE_FILENAME(int popId) => $"clone_tree{PopSuffix(popId)}.dot";
    private static string NEWICK_TREE_FILENAME(int popId) => $"clone_tree{PopSuffix(popId)}.new";
    private static string BIN_DOT_TREE_FILENAME(int popId) => $"bin_tree{PopSuffix(popId)}.dot";
    private static string BIN_NEWICK_TREE_FILENAME(int popId) => $"bin_tree{PopSuffix(popId)}.new";
    private const string POPULATIONS_DF_FILENAME = "populations.csv";
    private const string ADJACENCY_DF_FILENAME = "parent_tree.csv";
    private const string SIM_PARAMS_FILENAME = "sim_params.json";
    private const string SUMMARY_FILENAME = "summary.csv";

    private static string FormatNodeLabel(int id, long size)
        => $"{id}-{size}";

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

    public string Timestamp { get; }
    public string RootFolder { get; }
    public string ExperimentFolder { get; }
    public bool IsRepeated { get; }

    public void WriteClones(IEnumerable<Clone> clones, Dictionary<int, long> ccf, long alive, Dictionary<int, (int source, int dist)> parentMap, int popId = 0)
    {
        string outPath = Path.Combine(Path.GetFullPath(RootFolder), SUBCLONES_FILENAME(popId));
        using var outputFile = new StreamWriter(outPath);
        outputFile.WriteLine("ID,ParentID,Distance,Alive,Necrotic,Lost,Drivers,Passengers,Fitness,CCF");
        foreach (var clone in clones)
        {
            double frac = ccf[clone.CloneId] / (double) alive;
            // If no edge to parent is present, the clone is the root and parents itself.
            var parent = parentMap.TryGetValue(clone.CloneId, out var value) ? value : (clone.CloneId, 0);
            outputFile.WriteLine($"{clone.CloneId},{parent.Item1},{parent.Item2},{clone.AliveCount}," +
                                 $"{clone.NecroCount},{clone.LostCount},{clone.DriverCount},{clone.PassengersCount}," +
                                 $"{clone.Fitness},{frac:f4}");
        }
    }

    public void WriteDotTree(ListTree tree, int popId = 0)
    {
        string outPath = Path.Combine(Path.GetFullPath(RootFolder), DOT_TREE_FILENAME(popId));
        using var outputFile = new StreamWriter(outPath);

        outputFile.WriteLine("Digraph SMITH {");
        foreach (var node in tree.Nodes)
        {
            double size = Math.Round(.25 * (1 + Math.Log(1 + node.Size)), 2);
            outputFile.WriteLine($"\t{node.Id} [label=\"{FormatNodeLabel(node.Id, node.Size)}\", width={size}, height={size * .6}];");
        }

        foreach (var edge in tree.Edges)
        {
            outputFile.WriteLine($"\t{edge.SourceId} -> {edge.TargetId} [label=\"{edge.Distance}\"];");
        }

        outputFile.WriteLine("}");
    }

    private static void WriteDotNode(TreeNode node, StreamWriter outputFile)
    {
        double size = Math.Round(.25 * (1 + Math.Log(1 + node.Size)), 2);
        outputFile.WriteLine($"\t{node.Id} [label=\"{FormatNodeLabel(node.Id, node.Size)}\", width={size}, height={size * .6}];");
        foreach (var (child, distance) in node.Children)
        {
            WriteDotNode(child, outputFile);
            outputFile.WriteLine($"\t{node.Id} -> {child.Id} [label=\"{distance}\"];");
        }
    }

    private void WriteDotTreeByName(TreeNode tree, string fileName)
    {
        string outPath = Path.Combine(Path.GetFullPath(RootFolder), fileName);
        using var outputFile = new StreamWriter(outPath);

        outputFile.WriteLine("Digraph SMITH {");
        WriteDotNode(tree, outputFile);
        outputFile.WriteLine("}");
    }

    public void WriteBinDotTree(TreeNode tree, int popId = 0)
        => WriteDotTreeByName(tree, BIN_DOT_TREE_FILENAME(popId));

    private void WriteNode(TreeNode node, StreamWriter writer)
    {
        if (node.Children.Count > 0)
        {
            writer.Write("(");
            for (int i = 0; i < node.Children.Count; i++)
            {
                (var child, int distance) = node.Children[i];
                WriteNode(child, writer);
                writer.Write(":" + distance);
                // Write a comma unless this is the last child
                if (i < node.Children.Count - 1)
                {
                    writer.Write(",");
                }
            }
            writer.Write(")");
        }
        writer.Write(FormatNodeLabel(node.Id, node.Size));
    }
    
    private void WriteNewickTreeByName(TreeNode treeNode, string fileName)
    {
        string outPath = Path.Combine(Path.GetFullPath(RootFolder), fileName);
        using var writer = new StreamWriter(outPath);
        WriteNode(treeNode, writer);
        // Newick format requires a final semicolon
        writer.Write(";");
    }

    public void WriteNewickTree(TreeNode treeNode, int popId = 0)
        => WriteNewickTreeByName(treeNode, NEWICK_TREE_FILENAME(popId));

    public void WriteBinNewickTree(TreeNode treeNode, int popId = 0)
        => WriteNewickTreeByName(treeNode, BIN_NEWICK_TREE_FILENAME(popId));

    public void WriteMullerDataFrames(List<Clone> subClones, ListTree tree)
    {
        string popPath = Path.Combine(Path.GetFullPath(RootFolder), POPULATIONS_DF_FILENAME);
        string adjPath = Path.Combine(Path.GetFullPath(RootFolder), ADJACENCY_DF_FILENAME);

        using var popFile = new StreamWriter(popPath);
        popFile.WriteLine("Id,Step,Pop");
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
                    popFile.WriteLine($"{subClone.CloneId},{gen},{totalCells}");
                }
            }
        }

        using var adjFile = new StreamWriter(adjPath);
        adjFile.WriteLine("ParentId,ChildId");

        if (tree.Edges.Any())
        {
            foreach (var edge in tree.Edges)
            {
                adjFile.WriteLine($"{edge.SourceId},{edge.TargetId}");
            }
        }
        else
        {
            adjFile.WriteLine($"-1,{subClones.First().CloneId}");
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