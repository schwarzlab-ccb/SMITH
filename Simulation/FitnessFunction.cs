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
            case FitnessSampleType.Exponential:
                return Dist.ExponentialDistribution.Sample(rnd, simParams.FitnessMean);

            case FitnessSampleType.Normal:
                return Math.Max(Dist.NormalDistribution.Sample(rnd, simParams.FitnessMean, simParams.FitnessMean / 2.0), 0);

            case FitnessSampleType.Uniform:
                return Dist.ContinuousUniformDistribution.Sample(rnd, 0, simParams.FitnessMean * 2.0);

            case FitnessSampleType.Constant:
            default:
                return simParams.FitnessMean;
        }
    }
}