using SimChA.Computation;
using SimChA.DataTypes;
using ExtremeBinDist = Extreme.Statistics.Distributions.BinomialDistribution;

namespace SimChA.Simulation;

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
        var primeval = new SubClone(0, -1, 0, initFit, SimParams.StartMut, SimParams.StartPop);
        Clones = new List<SubClone> { primeval };
    }

    public List<SubClone> Clones { get; }
    public SimParams SimParams { get; }
    private Random Rnd { get; }

    private int GetNewId() => ++newId;

    private static double AccFitness(double original, double change, FitnessAccType type) 
        => type switch
        {
            FitnessAccType.Add => original + change,
            FitnessAccType.Mul => original * (1 + change),
            FitnessAccType.Eth => Math.Clamp(original * (1 + change * (1 - original / MAX_FIT)), 0.0, MAX_FIT),
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
    
    public void Step()
    {
        AliveSC = 0;
        StepNo++;

        List<SubClone> newClones = new();
        var popState = CellSampling.PopState(Clones);

        double unconfined = popState.Tumor;
        if (SimParams.Confinement > 0)
        {
            double r = Math.Pow(3.0 / 4.0 * (popState.Tumor / Math.PI), 1.0 / 3.0);
            double reminder = r - 1.0 / SimParams.Confinement;
            if (reminder > 0)
            {
                double blockedPop = 4.0 / 3.0 * Math.PI * Math.Pow(reminder, 3.0);
                unconfined = popState.Tumor - blockedPop;
            }
        }

        double divFraction = popState.Alive > unconfined && popState.Alive > 0
            ? Math.Clamp(unconfined / popState.Alive, 0.0, 1.0)
            : 1.0;

        foreach (var subClone in Clones.Where(sc => sc.AliveCount > 0))
        {
            AliveSC++;

            // Kill cells
            double deathFit = GetDeath(subClone.Fitness, SimParams.FitnessEffect);
            int newDead = ExtremeBinDist.Sample(Rnd, (int)subClone.AliveCount, deathFit * SimParams.Turnover);
            int newNecrotic = (int)Math.Round(newDead * (1 - divFraction));
            int disappeared = newDead - newNecrotic;

            // Create new cells
            double birthFit = GetBirth(subClone.Fitness, SimParams.FitnessEffect);
            double birthProb = Math.Clamp(birthFit * divFraction * SimParams.Turnover, 0.0, 1.0);
            int newCellsCount = ExtremeBinDist.Sample(Rnd, (int)subClone.AliveCount, birthProb);

            // Mutate some of the cells
            int newMutantCount =
                ExtremeBinDist.Sample(Rnd, newCellsCount, Math.Clamp(SimParams.MutationProb * 2, 0.0, 1.0));

            for (int mutationI = 0; mutationI < newMutantCount; mutationI++)
            {
                double divChange = FitnessFunction.SampleFitness(SimParams, Rnd);
                double newDivision = AccFitness(subClone.Fitness, divChange, SimParams.FitnessAcc);
                var childClone = subClone.CreateChild(GetNewId(), StepNo, newDivision, subClone.NumberDrivers + 1);
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