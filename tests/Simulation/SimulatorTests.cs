using SMITH.Computation;
using SMITH.DataTypes;
using SMITH.Simulation;
using Xunit;

namespace SMITH.Tests.Simulation;

public class SimulatorTests
{
    [Fact]
    public void Step_DoesNotCreateClonesPastConfiguredLimit()
    {
        var simParams = DeterministicMutationParams(startPop: 1000, maxClones: 10);
        var simulator = new Simulator(simParams, new Random(simParams.Seed));

        simulator.Step();

        Assert.Equal(simParams.MaxClones, simulator.Clones.Count);
        Assert.Equal(simParams.StartPop, CellSampling.PopState(simulator.Clones).Alive);
    }

    [Fact]
    public void Step_HandlesMutationSamplesAboveIntMaxValue()
    {
        const uint population = 2_147_483_648;
        var simParams = DeterministicMutationParams(population, maxClones: 2);
        var simulator = new Simulator(simParams, new Random(simParams.Seed));

        simulator.Step();

        Assert.Equal(2, simulator.Clones.Count);
        Assert.Equal((long)population - 1, simulator.Clones[0].AliveCount);
        Assert.Equal(1, simulator.Clones[1].AliveCount);
        Assert.Equal(population, CellSampling.PopState(simulator.Clones).Alive);
    }

    private static SimParams DeterministicMutationParams(uint startPop, int maxClones)
        => new()
        {
            Seed = 1,
            StartPop = startPop,
            Reps = 1,
            MaxPop = -1,
            MaxSteps = -1,
            MaxClones = maxClones,
            MinPop = 1,
            MaxTries = 1,
            Turnover = 1,
            MutationProb = 1,
            FitnessMean = 0,
            FitnessAcc = FitnessAccType.Add,
            FitnessDist = FitnessDistType.Constant,
            FitnessEffect = FitnessEffectType.Birth,
            CutOff = 0,
            CloneSample = -1
        };
}
