namespace SMITH.DataTypes;

public class Clone
{
    public Clone(int cloneId, int parentId, int generation, double fitness, uint driverCount, long popSize)
    {
        CloneId = cloneId;
        ParentId = parentId;
        DriverCount = driverCount;
        Fitness = fitness;
        FirstGen = generation;
        Cells = new List<(long Alive, long Necro)> { (popSize, 0) };
    }

    public int FirstGen { get; }
    public int CloneId { get; }
    public int ParentId { get; }
    public double Fitness { get; }
    public uint DriverCount { get; }
    private List<(long Alive, long Necro)> Cells { get; }

    public long AliveCount => Cells.Last().Alive;

    public long NecroCount => Cells.Last().Necro;

    public long CellCount => AliveCount + NecroCount;


    public int LastGen => FirstGen + Cells.Count;

    public long LostCount { get; private set; }

    public Clone CreateChild(int newId, int generation, double fitness, uint numberDrivers)
        => new(newId, CloneId, generation, fitness, numberDrivers, 1);

    public static string Header()
        => "ID,Parent,Alive,Necrotic,Lost,Drivers,Fitness";

    public override string ToString()
        => $"{CloneId},{ParentId},{AliveCount},{NecroCount},{LostCount},{DriverCount},{Fitness}";

    public long AliveAtGen(int gen)
        => gen >= FirstGen && gen < LastGen ? Cells[gen - FirstGen].Alive : 0;

    public void NewGen(uint genAlive, uint genDead, uint genDis)
    {
        Cells.Add((genAlive, genDead));
        LostCount += genDis;
    }
}