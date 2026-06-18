using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Infrastructure;


public sealed record SimulationConfig(
    Difficulty Difficulty,
    AircraftConfig Aircraft,
    RouteConfig Route,
    string LogFilePath = "blackbox_log.txt",
    bool RealTime = true,
    double TimeStepDeltaT = 0.1
)
{
    // Wyliczanie parametrów na podstawie wybranego poziomu trudności
    public double AnomalyChancePerTick => Difficulty switch
    {
        Difficulty.Easy => 0.003,
        Difficulty.Normal => 0.008,
        _ => 0.015
    };
    
    public double CriticalAnomalyChance => Difficulty switch
    {
        Difficulty.Easy => 0.01,
        Difficulty.Normal => 0.05,
        _ => 0.12
    };
    
    public double CascadeProbabilityMultiplier => Difficulty switch
    {
        Difficulty.Easy => 0.4,
        Difficulty.Normal => 1.0,
        _ => 1.6
    };

    public double MaxWindSpeedKnots => Difficulty switch
    {
        Difficulty.Easy => 12.0,
        Difficulty.Normal => 40.0,
        _ => 70.0
    };
    
    public double TurbulenceSensorFaultChance => Difficulty switch
    {
        Difficulty.Easy => 0.01,
        Difficulty.Normal => 0.07,
        _ => 0.20
    };
}