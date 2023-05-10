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
        var primeval = new Clone(0, -1, 0, initFit, SimParams.StartMut, 0u, SimParams.StartPop);
        Clones = new List<Clone> { primeval };
    }

    public List<Clone> Clones { get; }
    public SimParams SimParams { get; }
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

        List<Clone> newClones = new();
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
            int newDead = ExtremeBinDist.Sample(Rnd, (int)subClone.AliveCount, deathFit * SimParams.Turnover);
            int disappeared = ExtremeBinDist.Sample(Rnd, newDead, cloneFrac);
            int newNecrotic = newDead - disappeared;

            // Create new cells
            double birthFit = GetBirth(subClone.Fitness, SimParams.FitnessEffect);
            double birthProb = Math.Clamp(birthFit * SimParams.Turnover, 0.0, 1.0);
            int newCellsCount;
            if (subClone.AliveCount > 1_000_000_000)
            {
                double frac = 1_000_000_000.0 / subClone.AliveCount;
                newCellsCount = (int)(ExtremeBinDist.Sample(Rnd, (int) (frac * subClone.AliveCount), birthProb * cloneFrac) / frac);
            }
            else
            {
                newCellsCount = ExtremeBinDist.Sample(Rnd, (int)subClone.AliveCount, birthProb * cloneFrac);
            }
                

            // Mutate some of the cells
            int newMutantCount = ExtremeBinDist.Sample(Rnd, newCellsCount, SimParams.MutationProb);

            for (int mutationI = 0; mutationI < newMutantCount; mutationI++)
            {
                bool isDriver = Rnd.NextDouble() < SimParams.DriverProb;
                double divChange = isDriver ? FitnessFunction.SampleFitness(SimParams, Rnd) : 0;
                double newDivision = AccFitness(subClone.Fitness, divChange, SimParams.FitnessAcc);
                uint newDrivers = subClone.DriverCount + (isDriver ? 1u : 0u);
                uint newPassengers = subClone.PassengersCount + (isDriver ? 0u : 1u);
                var childClone = subClone.CreateChild(GetNewId(), StepNo, newDivision, newDrivers, newPassengers);
                newClones.Add(childClone);
            }

            subClone.NewGen(
                (uint)(subClone.AliveCount + newCellsCount - newMutantCount - newDead),
                (uint)(subClone.NecroCount + newNecrotic),
                (uint)disappeared);
        }

        Clones.AddRange(newClones);
    }
}