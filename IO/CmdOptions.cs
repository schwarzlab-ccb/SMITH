using CommandLine;

namespace SimChA.IO;

public class CmdOptions
{
    [Option('O', "output", Required = false, Default = "../out", 
        HelpText = "The path to the output files.")]
    public string OutputPath { get; set; }

    [Option('C', "config", Required = false, Default = "", 
        HelpText = "A json file with configuration of the experiment.")]
    public string ConfigFile { get; set; }

    [Option('N', Required = false, Default = false,
        HelpText = "Use newline in logs (useful for batch execution)")]
    public bool Newline { get; set; }
}