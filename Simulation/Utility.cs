// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SimChA.DataTypes;

namespace SimChA.Simulation;

public static class Utility
{
    public static List<uint> CreateCheckpoints(SimParams simParams)
    {
        if (!simParams.Checkpoints)
        {
            return new List<uint>();
        }

        var checkpoints = new List<uint> { simParams.MinPop };
        while (checkpoints.Last() < simParams.MaxPop)
        {
            checkpoints.Add(checkpoints.Last() * 2);
        }

        return checkpoints;
    }
}