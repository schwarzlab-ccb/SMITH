// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SimChA.Computation;
using static System.String;

namespace SimChA.DataTypes;

public struct ResultSummary
{
    public int RepeatId;
    public int GenerationId;
    public int Generations;
    public string Time;

    public int SubcloneSelect;
    public int SubcloneAlive;
    public int SubcloneTotal;

    public long CellSelectCount;
    public long CellAliveCount;
    public long CellNecroCount;
    public long CellTumorCount;
    public long CellLostCount;
    public long CellTotalCount;

    public double MeanDriversPerCell;
    public double ClonalDiversity;
    public double TreeBalance;
    public int TreeDepth;
    public int NodeCount;
    public int LeafCount;
    public float Branching;

    public override string ToString()
        => ToString(this);

    private static string ToString(ResultSummary rs)
        => Join(",", rs.GetType().GetFields().Select(f => f.GetValue(rs).ToString()));

    public static string Header()
        => Join(",", typeof(ResultSummary).GetFields().Select(f => f.Name));

    public string ToText()
        => "\t" + Join(",\n\t",
            Header().Split(",").Zip(ToString().Split(","), (label, val) => $"{label}: {val}"));


    public ResultSummary(int repeatId, int generationId, int stepNo, string timeElapsed, ParentTree connectedTree,
        List<SubClone> aboveCutOff, List<SubClone> clones, PopulationState popState)
    {
        RepeatId = repeatId;
        GenerationId = generationId;
        Time = timeElapsed;
        Generations = stepNo;

        SubcloneTotal = clones.Count;
        SubcloneSelect = aboveCutOff.Count;
        SubcloneAlive = clones.Count(sc => sc.AliveCount > 0);

        CellTotalCount = popState.Total;
        CellTumorCount = popState.Tumor;
        CellAliveCount = popState.Alive;
        CellLostCount = popState.Lost;
        CellNecroCount = popState.Necro;
        CellSelectCount = aboveCutOff.Sum(sc => sc.AliveCount);

        (NodeCount, LeafCount, TreeDepth, Branching)
            = TreeAnalysis.ComputeTreeSize(connectedTree);
        TreeBalance = TreeAnalysis.ComputeTreeBalance(LeafCount, connectedTree);
        ClonalDiversity = TreeAnalysis.ComputeClonalDiversity(aboveCutOff);
        MeanDriversPerCell = TreeAnalysis.ComputeMeanDriversPerCell(aboveCutOff);
    }
}