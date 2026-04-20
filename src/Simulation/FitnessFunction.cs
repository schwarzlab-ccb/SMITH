// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SMITH.DataTypes;
using Dist = Extreme.Statistics.Distributions;

namespace SMITH.Simulation;

public static class FitnessFunction
{
    public static double SampleFitness(SimParams simParams, Random rnd)
    {
        switch (simParams.FitnessDist)
        {
            case FitnessDistType.Exponential:
                return Dist.ExponentialDistribution.Sample(rnd, simParams.FitnessMean);

            case FitnessDistType.Normal:
                return Math.Max(Dist.NormalDistribution.Sample(rnd, simParams.FitnessMean, simParams.FitnessMean / 2.0), 0);

            case FitnessDistType.Uniform:
                return Dist.ContinuousUniformDistribution.Sample(rnd, 0, simParams.FitnessMean * 2.0);

            case FitnessDistType.Constant:
            default:
                return simParams.FitnessMean;
        }
    }
}