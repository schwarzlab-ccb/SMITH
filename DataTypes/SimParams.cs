// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SimChA.DataTypes;

[Serializable]
public struct SimParams
{
    // Simulator
    public int Seed;
    public int StartMut;
    public int StartPop;
    public uint Reps;

    // Experiment
    public long MaxPop; // Stop when this population is reached, can be negative to disable
    public uint MaxSteps; // Stop when this many simulation steps are reached, can be negative to disable
    public int MaxClones; // Stop when this many clones are found, can be negative to disable
    public uint MinPop; // If the sample size of MinPop is not reach, simulation restarts
    public double MaxTries; // Maximum number of tries. Note that this will cause incomplete results.
    
    // Model
    public double Turnover;
    public double MutationProb;
    public double DriverProb; // Likelihood that a mutation is a driver mutation
    public double FitnessMean;
    public double ConfGlobal;
    public double ConfLocal; 

    // Function    
    public FitnessAccType FitnessAcc;
    public FitnessSampleType FitnessDist;
    public FitnessEffectType FitnessEffect;

    // Output
    public bool Checkpoints; // Store meta-results in summary at every cell doubling 
    public double CutOff; // Minimum sample size compared to the total sample size for a clone to be considered
    public int CloneSample; // Limits the number of clones after cut-off, can be negative to disable
    public double FishFrac; // Minimum fraction of the population that must be sampled at any timepoint to be considered a clone
}