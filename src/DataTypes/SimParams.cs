// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SMITH.DataTypes;

[Serializable]
public struct SimParams
{
    // Simulator
    public int Seed;
    public uint StartMut;
    public uint StartPop;
    public uint Reps;

    // Experiment
    public long MaxPop; // Stop when this population is reached, can be negative to disable
    public int MaxSteps; // Stop when this many simulation steps are reached, can be negative to disable
    public int MaxClones; // Stop when this many clones are found, can be negative to disable
    public uint MinPop; // If the sample size of MinPop is not reach, simulation restarts
    public int MaxTries; // Maximum number of tries. Note that this will cause incomplete results, can be negative to disable
    
    // Model
    public double Turnover;
    public double MutationProb; // Probability of a driver mutation per new cell
    public double FitnessMean;
    public double ConfGlobal;
    public double ConfLocal; 

    // Function    
    public FitnessAccType FitnessAcc;
    public FitnessDistType FitnessDist;
    public FitnessEffectType FitnessEffect;

    // Output
    public bool Checkpoints; // Store meta-results in summary at every cell doubling 
    public double CutOff; // Minimum sample size compared to the total sample size for a clone to be considered
    public int CloneSample; // Limits the number of clones after cut-off, can be negative to disable
    public bool CalcFish; // Adds the fish data if true
    public double FishFrac; // Minimum fraction of the population that must be sampled at any timepoint to be considered a clone

    public string SanityCheck()
    {
        if (Turnover is <= 0 or > 1)
        {
            return "Turnover must be in (0, 1]";
        }
        if (MutationProb is < 0 or > 1)
        {
            return "Mutation probability must be in [0, 1]";
        }
        if (FitnessMean < 0)
        {
            return "Fitness mean must be non-negative";
        }
        if (ConfGlobal < 0)
        {
            return "ConfGlobal mean must be non-negative";
        }
        if (ConfLocal < 0)
        {
            return "ConfLocal mean must be non-negative";
        }
        if (CutOff is < 0 or > 1)
        {
            return "CutOff must be in [0, 1]";
        }
        if (FishFrac is < 0 or > 1)
        {
            return "CutOff must be in [0, 1]";
        }
        return "";
    }
}