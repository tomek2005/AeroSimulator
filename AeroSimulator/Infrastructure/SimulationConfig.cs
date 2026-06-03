namespace AeroSimulator.Infrastructure;

public class SimulationConfig
{
    public double TimeStepDeltaT { get; set; } = 0.1;
    public double TotalSimulationTimeLimit { get; set; } = 300.0;
    public string LogFilePath { get; set; } = "blackbox_log.csv";
    public double AnomalyProbabilityPerSecond { get; set; } = 0.02;
    public bool RealTimeSimulation { get; set; } = true;
}