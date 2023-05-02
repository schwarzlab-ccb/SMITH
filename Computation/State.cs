// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SMITH.DataTypes;
using SMITH.Simulation;

namespace SMITH.Computation;

public static class State
{
    public static ComputeState GetCompState(PopState state, Simulator simulator, SimParams simParams)
    {
        if (simulator.StepNo >= simParams.MaxSteps && simParams.MaxSteps > 0)
        {
            return ComputeState.Finished;
        }
        if (simulator.Clones.Count >= simParams.MaxClones && simParams.MaxClones > 0)
        {
            return ComputeState.Finished;
        }
        if (state.Alive <= 0)
        {
            return state.Tumor > simParams.MinPop ? ComputeState.Finished : ComputeState.Reset;
        }
        if (state.Tumor >= simParams.MaxPop  && simParams.MaxPop > 0)
        {
            return ComputeState.Finished;
        }
        return ComputeState.Running;
    }
    
    public static string StateLog(int repeatId, int tryNo, Simulator simulator, SimParams simParams, List<PopState> popStates)
    {
        double prog = (double)popStates.Last().Tumor / simParams.MaxPop;
        return $"sim: {repeatId + 1}.{tryNo}/{simParams.Reps}, " +
               $"step: {simulator.StepNo:D3}, " +
               $"prog: {prog:P}, " +
               $"SC_total: {simulator.Clones.Count}, " +
               $"SC_alive: {simulator.AliveSC}, " +
               $"C_alive: {popStates.Last().Alive:N0}, " +
               $"C_necro: {popStates.Last().Necro:N0}, " +
               $"C_lost: {popStates.Last().Lost:N0}, " +
               $"Frac: {simulator.GlobalFrac:F2}";
    }

    public static (List<SubClone> subClones, ListTree tree) GetMullerData(Simulator simulator, SimParams simParams, List<PopState> popStates)
    {
        if (simParams.PlotFish)
        {
            var mullerSelect = popStates.Select(pair => pair.Alive * simParams.FishFrac).ToList();
            int firstPop = mullerSelect.FindIndex(minPop => minPop > 0);
            if (firstPop >= 0)
            {
                var mullerPops = simulator
                    .Clones
                    .Where(sc => sc.FirstGen <= firstPop || Enumerable.Range(firstPop, popStates.Count)
                        .Any(g => mullerSelect[g] <= sc.AliveAtGen(g)))
                    .ToList();
                var mullerTree = TreeBuilder.BuildCTree(simulator.Clones, mullerPops);
                return (mullerPops, mullerTree);
            }
        }
        return (new List<SubClone>(), new ListTree());
    }
}