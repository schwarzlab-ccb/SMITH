using CommandLine;
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
    var runner = new SimulationRunner(simParams, files, random, options.Value.Newline);
    runner.RunAll();
}
catch (Exception e)
{
    Console.WriteLine($"Failed with exception {e.Message}. Stack: {e.StackTrace}");
    return e.HResult;
}

return 0;