

using AeroSimulator.Core.Strategies.Anomalies;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Controllers;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;
public class AnomalyEngine
{
    private readonly Aircraft _aircraft;
    private readonly Random _rng = new();
    
    // Lista bazowych anomalii, które MOGĄ wylosować się same (mają Probability > 0)
    private readonly List<string> _poolOfRandomAnomalies = new()
    {
        "BIRD_STRIKE", "HYDRAULIC_FAILURE", "FUEL_LEAK", 
        "ELECTRICAL_FAILURE", "DECOMPRESSION", "TURBULENCE", "ICING",
        "MICROBURST", "RUNWAY_INCURSION", "SENSOR_FAILURE"
    };

    public List<IAnomaly> ActiveAnomalies { get; } = new();

    public AnomalyEngine(Aircraft aircraft)
    {
        _aircraft = aircraft ?? throw new ArgumentNullException(nameof(aircraft));
    }

    public void Tick(double deltaT)
    {
        // 1. Losowanie automatycznych awarii ze środowiska
        RollEnvironmentAnomalies(deltaT);

        // 2. Bezpieczna pętla aktualizacyjna
        for (int i = ActiveAnomalies.Count - 1; i >= 0; i--)
        {
            var anomaly = ActiveAnomalies[i];
            if (anomaly.IsActive)
            {
                anomaly.Update(_aircraft, _aircraft.FlightData, deltaT);
            }
            else
            {
                ActiveAnomalies.RemoveAt(i);
            }
        }
    }

    private void RollEnvironmentAnomalies(double deltaT)
    {
        foreach (var typeCode in _poolOfRandomAnomalies)
        {
            // Sprawdzenie unikalności po rzeczywistym typie klasy, a nie po zmiennym stringu!
            if (IsAnomalyAlreadyActive(typeCode)) continue;

            var blueprint = AnomalyFactory.CreateAnomaly(typeCode);
            if (_rng.NextDouble() < (blueprint.Probability * deltaT))
            {
                blueprint.Trigger(_aircraft, _aircraft.FlightData);
                ActiveAnomalies.Add(blueprint);
            }
        }
    }

    /// <summary>
    /// Metoda centralnie wywoływana również przez EventBus przy kaskadach!
    /// Np. gdy BirdStrike krzyczy: CASCADE:ENGINE_FIRE:1
    /// </summary>
    public void TriggerDirectAnomaly(string typeCode, int componentIndex)
    {
        // Blokada: Nie odpalamy drugiego pożaru na TYM SAMYM silniku
        if (ActiveAnomalies.OfType<EngineFireAnomaly>().Any(a => a.EngineIndex == componentIndex))
        {
            return; 
        }

        var cascadeAnomaly = AnomalyFactory.CreateAnomaly(typeCode, componentIndex);
        cascadeAnomaly.Trigger(_aircraft, _aircraft.FlightData);
        ActiveAnomalies.Add(cascadeAnomaly);
    }

    public bool TryResolveActiveAnomaly()
    {
        foreach (var anomaly in ActiveAnomalies.Where(a => a.IsActive && a.CanBeResolved).ToList())
        {
            if (anomaly.Resolve(_aircraft))
            {
                ActiveAnomalies.Remove(anomaly);
                return true;
            }
        }

        return false;
    }

private bool IsAnomalyAlreadyActive(string typeCode)
{
    // Mapowanie typów dla bezpieczeństwa Enterprise
    return typeCode switch
    {
        "BIRD_STRIKE" => ActiveAnomalies.Any(a => a is BirdStrikeAnomaly),
        "HYDRAULIC_FAILURE" => ActiveAnomalies.Any(a => a is HydraulicFailureAnomaly),
        "FUEL_LEAK" => ActiveAnomalies.Any(a => a is FuelLeakAnomaly),
        "MICROBURST" => ActiveAnomalies.Any(a => a is MicroburstAnomaly),
        "RUNWAY_INCURSION" => ActiveAnomalies.Any(a => a is RunwayIncursionAnomaly),
        "SENSOR_FAILURE" => ActiveAnomalies.Any(a => a is SensorFailureAnomaly),
        "ELECTRICAL_FAILURE" => ActiveAnomalies.Any(a => a is ElectricalFailureAnomaly),
        "DECOMPRESSION" => ActiveAnomalies.Any(a => a is DecompressionAnomaly),
        "TURBULENCE" => ActiveAnomalies.Any(a => a is TurbulenceAnomaly),
        "ICING" => ActiveAnomalies.Any(a => a is IcingAnomaly),
        
        _ => false
    };
}
}
