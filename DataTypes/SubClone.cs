namespace SimChA.DataTypes;

public class SubClone
{
    public SubClone(int cloneId, int parentId, int generation, double fitness, int numberDrivers, long popSize)
    {
        CloneId = cloneId;
        ParentId = parentId;
        NumberDrivers = numberDrivers;
        Fitness = fitness;
        FirstGen = generation;
        Cells = new List<(long Alive, long Necro)> { (popSize, 0) };
    }

    public int FirstGen { get; }
    public int CloneId { get; }
    public int ParentId { get; }
    public double Fitness { get; }
    public int NumberDrivers { get; }
    private List<(long Alive, long Necro)> Cells { get; }

    public long AliveCount => Cells.Last().Alive;

    public long NecroCount => Cells.Last().Necro;

    public int LastGen => FirstGen + Cells.Count;

    public long LostCount { get; private set; }

    public SubClone CreateChild(int newId, int generation, double fitness, int numberDrivers)
        => new(newId, CloneId, generation, fitness, numberDrivers, 1);

    public override string ToString()
        => $"ID:{CloneId}, Parent:{ParentId}, Alive: {AliveCount}, Necrotic: {NecroCount}, Lost: {LostCount}, " +
           $"Drivers: {NumberDrivers}, Fitness: {Fitness}";

    public long AliveAtGen(int gen)
        => gen >= FirstGen && gen < LastGen ? Cells[gen - FirstGen].Alive : 0;

    public void NewGen(uint genAlive, uint genDead, uint genDis)
    {
        Cells.Add((genAlive, genDead));
        LostCount += genDis;
    }
}