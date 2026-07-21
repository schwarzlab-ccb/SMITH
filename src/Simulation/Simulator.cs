using SMITH.Computation;
using SMITH.DataTypes;
using ExtremeBinDist = Extreme.Statistics.Distributions.BinomialDistribution;

namespace SMITH.Simulation;

public class Simulator
{
    private const double MAX_FIT = 10.0;

    public int AliveSC;

    private int newId;

    public int StepNo;

    public Simulator(SimParams simParams, Random rnd)
    {
        StepNo = 0;
        SimParams = simParams;
        Rnd = rnd;

        double initFit = 1;
        for (int i = 0; i < SimParams.StartMut; i++)
        {
            double sample = FitnessFunction.SampleFitness(simParams, rnd);
            initFit = AccFitness(initFit, sample, SimParams.FitnessAcc);
        }
        var primeval = new Clone(0, -1, 0, initFit, SimParams.StartMut, SimParams.StartMut, SimParams.StartPop);
        Clones = [primeval];
    }

    public List<Clone> Clones { get; }
    private SimParams SimParams { get; }
    private Random Rnd { get; }
    public double GlobalFrac { get; private set; }

    private int GetNewId() => ++newId;

    private static double AccFitness(double original, double change, FitnessAccType type) 
        => type switch
        {
            FitnessAccType.Add => original + change,
            FitnessAccType.Mul => original * (1 + change),
            FitnessAccType.Lim => Math.Clamp(original * (1 + change * (1 - original / MAX_FIT)), 0.0, MAX_FIT),
            _ => original
        };

    private static double GetBirth(double fitness, FitnessEffectType effect)
        => effect switch {
            FitnessEffectType.Birth => fitness,
            FitnessEffectType.Death => 1,
            FitnessEffectType.Both => (fitness + 1) / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null)
        };
    
    private static double GetDeath(double fitness, FitnessEffectType effect)
        => effect switch {
            FitnessEffectType.Death => 1 / fitness,
            FitnessEffectType.Birth => 1,
            FitnessEffectType.Both => 2 / (fitness + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null)
        };

    private static double CalcFree(long totalPop, double conf)
    {
        if (conf > 0)
        {
            double r = Math.Pow(3.0 / 4.0 * (totalPop / Math.PI), 1.0 / 3.0);
            double reminder = r - 1.0 / conf;
            if (reminder > 0)
            {
                double blockedPop = 4.0 / 3.0 * Math.PI * Math.Pow(reminder, 3.0);
                return totalPop - blockedPop;
            }
        }
        return totalPop;
    }

    private static double CalcFraction(long aliveCount, double freeCount) => 
        aliveCount > freeCount && aliveCount > 0 ? Math.Clamp(freeCount / aliveCount, 0.0, 1.0) : 1.0;
    
    public void Step()
    {
        AliveSC = 0;
        StepNo++;

        List<Clone> newClones = [];
        var popState = CellSampling.PopState(Clones);

        double globalFree = CalcFree(popState.Alive + popState.Necro, SimParams.ConfGlobal);
        GlobalFrac = CalcFraction(popState.Alive, globalFree);

        foreach (var subClone in Clones.Where(sc => sc.AliveCount > 0))
        {
            AliveSC++;

            double localFree = CalcFree(subClone.CellCount, SimParams.ConfLocal);
            double cloneFrac = CalcFraction(subClone.AliveCount, localFree) * GlobalFrac;

            // Kill cells
            double deathFit = GetDeath(subClone.Fitness, SimParams.FitnessEffect);
            long newDead = SampleBinomial(Rnd, subClone.AliveCount, deathFit * SimParams.Turnover);
            long disappeared = SampleBinomial(Rnd, newDead, cloneFrac);
            long newNecrotic = newDead - disappeared;

            // Create new cells
            double birthFit = GetBirth(subClone.Fitness, SimParams.FitnessEffect);
            double birthProb = Math.Clamp(birthFit * SimParams.Turnover, 0.0, 1.0);
            long newCellsCount = SampleBinomial(Rnd, subClone.AliveCount, birthProb * cloneFrac);

            // Mutate some of the cells
            long newMutantCount = SampleBinomial(Rnd, newCellsCount, SimParams.MutationProb);
            if (SimParams.MaxClones > 0)
            {
                long remainingCloneCapacity = Math.Max(
                    0L, (long)SimParams.MaxClones - Clones.Count - newClones.Count);
                newMutantCount = Math.Min(newMutantCount, remainingCloneCapacity);
            }

            long driverMutantCount = 0;
            for (long mutationI = 0; mutationI < newMutantCount; mutationI++)
            {
                double divChange = FitnessFunction.SampleFitness(SimParams, Rnd);
                double newDivision = AccFitness(subClone.Fitness, divChange, SimParams.FitnessAcc);
                var childClone = subClone.CreateChild(
                    GetNewId(), StepNo, newDivision, subClone.DriverCount + 1u, 1);
                newClones.Add(childClone);
                driverMutantCount++;
            }

            subClone.NewGen(
                subClone.AliveCount + newCellsCount - driverMutantCount - newDead,
                subClone.NecroCount + newNecrotic,
                disappeared);
        }

        Clones.AddRange(newClones);
    }

    private static long SampleBinomial(Random rnd, long n, double p)
    {
        if (n <= 0) return 0;

        const int maxChunkSize = 1_000_000_000;
        long sample = 0;
        while (n > 0)
        {
            int chunkSize = (int)Math.Min(n, maxChunkSize);
            sample += ExtremeBinDist.Sample(rnd, chunkSize, p);
            n -= chunkSize;
        }

        return sample;
    }
}
