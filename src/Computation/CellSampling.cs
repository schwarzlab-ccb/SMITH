// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SMITH.DataTypes;

namespace SMITH.Computation;

public static class CellSampling
{
    public static PopState PopState(IEnumerable<Clone> population)
    {
        var popState = new PopState();
        foreach (var sc in population)
        {
            popState.Alive += sc.AliveCount;
            popState.Necro += sc.NecroCount;
            popState.Lost += sc.LostCount;
        }
        popState.Tumor = popState.Alive + popState.Necro;
        popState.Total = popState.Tumor + popState.Lost;
        return popState;
    }
}