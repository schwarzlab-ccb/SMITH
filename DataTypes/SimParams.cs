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
    public long MaxPop;
    public uint MaxSteps;
    public int MaxClones;
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