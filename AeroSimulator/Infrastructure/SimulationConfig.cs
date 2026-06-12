using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Infrastructure;

/// <summary>
/// Niezmienny obiekt stanowy (Record) przechowujący wszystkie parametry bieżącej symulacji.
/// Jest przekazywany z widoku Menu Głównego do Kontrolera Gry.
/// </summary>
public sealed record SimulationConfig(
    Difficulty Difficulty,
    AircraftConfig Aircraft,
    RouteConfig Route,
    string LogFilePath = "blackbox_log.txt", // Domyślna ścieżka do zapisu (z README)
    bool RealTime = true,                    // Czy symulacja jest w czasie rzczywistym (10Hz)
    double TimeStepDeltaT = 0.1              // 10 ticków na sekundę = 0.1s na tick
)
{
    // =========================================================================
    // PARAMETRY WYLICZANE DYNAMICZNIE NA PODSTAWIE POZIOMU TRUDNOŚCI
    // =========================================================================

    /// <summary>Szansa na wygenerowanie anomalii podczas ticku AnomalyEngine.</summary>
    public double AnomalyChancePerTick => Difficulty switch
    {
        Difficulty.Easy => 0.003,
        Difficulty.Normal => 0.008,
        _ => 0.015
    };

    /// <summary>Prawdopodobieństwo wylosowania poziomu krytycznego anomalii.</summary>
    public double CriticalAnomalyChance => Difficulty switch
    {
        Difficulty.Easy => 0.01,
        Difficulty.Normal => 0.05,
        _ => 0.12
    };

    /// <summary>Mnożnik szans aktywacji kaskad awarii (np. ogień przechodzi na skrzydło).</summary>
    public double CascadeProbabilityMultiplier => Difficulty switch
    {
        Difficulty.Easy => 0.4,
        Difficulty.Normal => 1.0,
        _ => 1.6
    };

    /// <summary>Maksymalna siła wiatru generowana przez algorytmy pogodowe (węzły).</summary>
    public double MaxWindSpeedKnots => Difficulty switch
    {
        Difficulty.Easy => 12.0,
        Difficulty.Normal => 40.0,
        _ => 70.0
    };

    /// <summary>Szansa na uszkodzenie losowego czujnika przy silnych turbulencjach.</summary>
    public double TurbulenceSensorFaultChance => Difficulty switch
    {
        Difficulty.Easy => 0.01,
        Difficulty.Normal => 0.07,
        _ => 0.20
    };
}