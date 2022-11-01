// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SimChA.DataTypes;

namespace SimChA.Simulation;

public static class Utility
{
    public static List<long> CreateCheckpoints(SimParams simParams)
    {
        if (!simParams.Checkpoints)
        {
            return new List<long>();
        }

        var checkpoints = new List<long> { simParams.MinPop };
        while (checkpoints.Last() < simParams.MaxPop)
        {
            checkpoints.Add(checkpoints.Last() * 2L);
        }

        return checkpoints;
    }
}