using System.Diagnostics;
using CommandLine;
using SMITH.Computation;
using SMITH.DataTypes;
using SMITH.IO;
using SMITH.Simulation;

var options = Parser.Default.ParseArguments<CmdOptions>(args);
options.WithNotParsed(o =>
{
    Console.WriteLine("Exiting");
    o.ToList().ForEach(Console.Write); // Write out errors
    Environment.Exit(1);
});

string paramsPath = options.Value.ConfigFile != "" ? options.Value.ConfigFile : "./sim_params.json";
var simParams = FileIO.SimParamsFromFile(paramsPath);
string checkResult = simParams.SanityCheck();
if (checkResult != "")
{
    throw new Exception($"Failed sanity check with error: {checkResult}");
}

var random = new Random(simParams.Seed);
FileIO files;
try
{
    bool isRepeated = simParams.Reps > 1;
    files = new FileIO(options.Value.OutputPath, isRepeated);
    files.WriteSimParams(simParams);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to write to disk with error: {e.Message}");
    return 2;
}

try
{
    var globalWatch = new Stopwatch();
    globalWatch.Start();

    int tryNo = 0;
    for (int repeatId = 0; repeatId < simParams.Reps; repeatId++)
    {
        var watch = new Stopwatch();
        watch.Start();

        // Simulation
        string lastLine = "";
        int checkpointId = 0;
        var simulator = new Simulator(simParams, random);
        var checkpoints = Utility.CreateCheckpoints(simParams);
        var popStates = new List<PopState> { CellSampling.PopState(simulator.Clones) };
        do
        {
            simulator.Step();
            popStates.Add(CellSampling.PopState(simulator.Clones));
            
            int lastLineLength = lastLine.Length; // Only pad what you need
            lastLine = State.StateLog(repeatId, tryNo, simulator, simParams, popStates);
            Console.Write(lastLine.PadRight(lastLineLength) + (options.Value.Newline ? "\n" : "\r"));

            if (State.GetCompState(popStates.Last(), simulator, simParams) == ComputeState.Finished
                || checkpoints.Any() && popStates.Last().Tumor > checkpoints[checkpointId])
            {
                // Analysis
                double cutOff = popStates.Last().Alive * simParams.CutOff;
                var aboveCutOff = simulator.Clones.Where(sc => sc.AliveCount >= cutOff).ToList();
                var cloneSample = (simParams.CloneSample > 0 && simParams.CloneSample < aboveCutOff.Count
                    ? aboveCutOff.OrderByDescending(c => c.AliveCount).Take(simParams.CloneSample)
                    : aboveCutOff).ToList();
                var lcaTreeList = TreeBuilder.BuildLCAT(simulator.Clones, cloneSample);
                var treeNodes = lcaTreeList.Nodes.Select(n => n.Id).ToList();
                var sample = simulator.Clones.Where(sc => treeNodes.Contains(sc.CloneId)).ToList();
                var CCF = TreeAnalysis.ComputeCCF(lcaTreeList);
                
                string time = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).ToString();
                var result = new ResultSummary(repeatId, checkpointId, simulator.StepNo, time,
                    lcaTreeList, cloneSample, simulator.Clones, popStates.Last(), CCF);
                files.AddToSummary(result);
                checkpointId++;

                // Result
                if (State.GetCompState(popStates.Last(), simulator, simParams) == ComputeState.Finished)
                {
                    files.WriteSubClones(sample);
                    files.WriteDotTree(lcaTreeList);
                    var tree = TreeBuilder.ListToTree(lcaTreeList);
                    files.WriteNewickTree(tree);
                    var (mullerPops, mullerTree) = State.GetMullerData(simulator, simParams, popStates);
                    if (mullerPops.Any())
                    {
                        files.WriteMullerDataFrames(mullerPops, mullerTree);
                    }
                    files.WriteCCF(CCF, popStates.Last().Alive);
                    files.StoreCopy(repeatId);
                    Console.WriteLine($"Sim: {repeatId + 1}.{tryNo}/{simParams.Reps} result:".PadRight(160));
                    Console.WriteLine(result.ToText());
                    GC.Collect();
                }
            }
        } while (State.GetCompState(popStates.Last(), simulator, simParams) == ComputeState.Running);

        // Skip on failure
        if (State.GetCompState(popStates.Last(), simulator, simParams) == ComputeState.Reset)
        {
            tryNo++;
            if (tryNo > simParams.MaxTries && simParams.MaxTries > 0)
            {
                throw new Exception($"On repeat {repeatId} exceeded {tryNo} tries.");
            }
            repeatId--;
        }
        else
        {
            tryNo = 0;
            watch.Stop();
            Console.WriteLine($"Execution Time: {TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds)}");
            Console.WriteLine(string.Join("", Enumerable.Repeat("*", 100)));
        }
    }

    files.CopySummary();
    globalWatch.Stop();

    Console.WriteLine($"Total time: {TimeSpan.FromMilliseconds(globalWatch.ElapsedMilliseconds)}");
    if (files.IsRepeated)
    {
        Console.WriteLine("Experiment ID:");
        Console.WriteLine(files.Timestamp);
    }
    
}
catch (Exception e)
{
    Console.WriteLine($"Failed with exception {e.Message}. Stack: {e.StackTrace}");
    return e.HResult;
}

return 0;