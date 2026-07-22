namespace SMITH.DataTypes;

public class Clone(
    int cloneId,
    int parentId,
    int generation,
    double fitness,
    uint drivers,
    uint distance,
    long popSize)
{
    public int FirstGen { get; } = generation;
    public int CloneId { get; } = cloneId;
    public int ParentId { get; } = parentId;
    public double Fitness { get; } = fitness;
    public uint DriverCount { get; } = drivers;
    public uint Distance { get;  } = distance;
    private List<(long Alive, long Necro)> Cells { get; } = [(popSize, 0)];

    public long AliveCount => Cells.Last().Alive;

    public long NecroCount => Cells.Last().Necro;

    public long CellCount => AliveCount + NecroCount;

    public int LastGen => FirstGen + Cells.Count;

    public long LostCount { get; private set; }

    public Clone CreateChild(int newId, int generation, double fitness, uint drivers, uint distance)
        => new(newId, CloneId, generation, fitness, drivers, distance, 1);

    public static string Header()
        => "ID,Parent,Distance,Alive,Necrotic,Lost,Drivers,Fitness";

    public override string ToString()
        => $"{CloneId},{ParentId},{Distance},{AliveCount},{NecroCount},{LostCount},{DriverCount},{Fitness}";

    public long AliveAtGen(int gen)
        => gen >= FirstGen && gen < LastGen ? Cells[gen - FirstGen].Alive : 0;

    public void NewGen(long genAlive, long genDead, long genDis)
    {
        Cells.Add((genAlive, genDead));
        LostCount += genDis;
    }
}