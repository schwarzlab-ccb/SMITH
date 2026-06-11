using CommandLine;
using SMITH.IO;
using SMITH.Simulation;

var parseResult = Parser.Default.ParseArguments<CmdOptions>(args);
if (parseResult is not Parsed<CmdOptions> parsedOptions)
{
    Console.WriteLine("Exiting");
    foreach (var error in parseResult.Errors)
    {
        Console.Write(error); // Write out errors
    }
    return 1;
}

var options = parsedOptions.Value;
string paramsPath = options.ConfigFile != "" ? options.ConfigFile : "./sim_params.json";
var simParams = FileIO.SimParamsFromFile(paramsPath);
string checkResult = simParams.SanityCheck();
if (checkResult != "")
{
    Console.WriteLine($"Failed sanity check with error: {checkResult}");
    return 2;
}

var random = new Random(simParams.Seed);
FileIO files;
try
{
    bool isRepeated = simParams.Reps > 1;
    files = new FileIO(options.OutputPath, isRepeated);
    files.WriteSimParams(simParams);
}
catch (Exception e)
{
    Console.WriteLine($"Failed to write to disk with error: {e.Message}");
    return 3;
}

try
{
    var runner = new SimulationRunner(simParams, files, random, options.Newline);
    runner.RunAll();
}
catch (Exception e)
{
    Console.WriteLine($"Failed with exception {e.Message}. Stack: {e.StackTrace}");
    return e.HResult;
}

return 0;