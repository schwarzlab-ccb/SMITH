// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SimChA.DataTypes;

[Serializable]
public struct SimParams
{
    // Simulator
    public int Seed;
    public int StartMut;
    public int StartPop;

    // Experiment
    public uint Reps;
    public long MaxPop; // Stop when this population is reached, can be negative to disable
    public uint MaxSteps; // Stop when this many simulation steps are reached, can be negative to disable
    public int MaxClones; // Stop when this many clones are found, can be negative to disable
    public uint MinPop;

    // Model
    public double Turnover;
    public double Confinement;
    public double MutationProb;
    public double DriverProb;
    public double FitnessMean;


    // Function    
    public FitnessAccType FitnessAcc;
    public FitnessSampleType FitnessDist;
    public FitnessEffectType FitnessEffect;

    // Output
    public bool Checkpoints;
    public int CloneSample;
    public double CutOff;
}