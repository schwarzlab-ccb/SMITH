using System.Diagnostics;
using SMITH.Computation;
using SMITH.DataTypes;
using SMITH.IO;

namespace SMITH.Simulation;

public class SimulationRunner
{
    private readonly SimParams _simParams;
    private readonly FileIO _files;
    private readonly Random _random;
    private readonly bool _logNewline;

    public SimulationRunner(SimParams simParams, FileIO files, Random random, bool logNewline)
    {
        _simParams = simParams;
        _files = files;
        _random = random;
        _logNewline = logNewline;
    }

    public void RunAll()
    {
        var globalWatch = Stopwatch.StartNew();

        int tryNo = 0;
        for (int repeatId = 0; repeatId < _simParams.Reps; repeatId++)
        {
            var watch = Stopwatch.StartNew();
            var finalState = RunSingleRepeat(repeatId, tryNo, watch);

            if (finalState == ComputeState.Reset)
            {
                tryNo++;
                if (tryNo > _simParams.MaxTries && _simParams.MaxTries > 0)
                {
                    throw new Exception($"On repeat {repeatId} exceeded {tryNo} tries.");
                }

                repeatId--;
                continue;
            }

            tryNo = 0;
            watch.Stop();
            Console.WriteLine($"Execution Time: {TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds)}");
            Console.WriteLine(string.Join("", Enumerable.Repeat("*", 100)));
        }

        _files.CopySummary();
        globalWatch.Stop();

        Console.WriteLine($"Total time: {TimeSpan.FromMilliseconds(globalWatch.ElapsedMilliseconds)}");
        Console.WriteLine($"Results written to {Path.GetFullPath(_files.ExperimentFolder)}");
    }

    private ComputeState RunSingleRepeat(int repeatId, int tryNo, Stopwatch watch)
    {
        string lastLine = "";
        int checkpointId = 0;

        var simulator = new Simulator(_simParams, _random);
        var checkpoints = Utility.CreateCheckpoints(_simParams);
        var popStates = new List<PopState> { CellSampling.PopState(simulator.Clones) };

        ComputeState compState;
        do
        {
            simulator.Step();
            var popState = CellSampling.PopState(simulator.Clones);
            popStates.Add(popState);

            int lastLineLength = lastLine.Length;
            lastLine = State.StateLog(repeatId, tryNo, simulator, _simParams, popStates);
            Console.Write(lastLine.PadRight(lastLineLength) + (_logNewline ? "\n" : "\r"));

            compState = State.GetCompState(popState, simulator, _simParams);
            bool checkpointReached = checkpoints.Any()
                                   && checkpointId < checkpoints.Count
                                   && popState.Tumor > checkpoints[checkpointId];
            if (compState == ComputeState.Finished || checkpointReached)
            {
                var elapsed =
                    TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
                var (result, lcaTreeList, sample, ccCount) = SimulationAnalysis.AnalyzeCheckpoint(
                    repeatId,
                    tryNo,
                    checkpointId,
                    simulator,
                    _simParams,
                    popState,elapsed);
                _files.AddToSummary(result);
                checkpointId++;

                if (compState == ComputeState.Finished)
                {
                    SimulationResultOutput.WriteFinishedOutputs(
                        _files,
                        result,
                        lcaTreeList,
                        sample,
                        ccCount,
                        popState.Alive,
                        simulator,
                        _simParams,
                        popStates,
                        repeatId,
                        tryNo);
                }
            }
        } while (compState == ComputeState.Running);

        return compState;
    }
}

