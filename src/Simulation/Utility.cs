// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SMITH.DataTypes;

namespace SMITH.Simulation;

public static class Utility
{
    public static List<long> CreateCheckpoints(SimParams simParams)
    {
        if (!simParams.Checkpoints)
        {
            return new List<long>();
        }
        if (simParams.MinPop == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(simParams.MinPop), "MinPop must be positive when checkpoints are enabled.");
        }

        var checkpoints = new List<long> { simParams.MinPop };
        while (checkpoints.Last() < simParams.MaxPop)
        {
            long last = checkpoints.Last();
            checkpoints.Add(last > simParams.MaxPop / 2 ? simParams.MaxPop : last * 2L);
        }

        return checkpoints;
    }
}
