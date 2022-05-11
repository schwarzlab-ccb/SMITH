// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SimChA.DataTypes;

namespace SimChA.Computation;

public static class CellSampling
{
    public static PopulationState PopState(List<SubClone> populations)
    {
        var popState = new PopulationState
        {
            Alive = populations.Sum(sc => sc.AliveCount),
            Necro = populations.Sum(sc => sc.NecroCount),
            Lost = populations.Sum(sc => sc.LostCount),
        };
        popState.Tumor = popState.Alive + popState.Necro;
        popState.Total = popState.Tumor + popState.Lost;
        return popState;
    }
}