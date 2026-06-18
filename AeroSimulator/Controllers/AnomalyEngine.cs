using AeroSimulator.Core.Strategies.Anomalies;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Controllers;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public class AnomalyEngine
{
    private readonly Aircraft _aircraft;
    private readonly Random _rng = new();
    
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
        RollEnvironmentAnomalies(deltaT);
        
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
            if (IsAnomalyAlreadyActive(typeCode)) continue;

            var blueprint = AnomalyFactory.CreateAnomaly(typeCode);
            if (_rng.NextDouble() < (blueprint.Probability * deltaT))
            {
                blueprint.Trigger(_aircraft, _aircraft.FlightData);
                ActiveAnomalies.Add(blueprint);
            }
        }
    }
    
    public void TriggerDirectAnomaly(string typeCode, int componentIndex)
    {
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
                _aircraft.PublishAlert(
                    $"REPAIR COMPLETE: {anomaly.AnomalyName} - {anomaly.Description}",
                    AeroSimulator.Core.Aircraft.Enums.Severity.Info);
                return true;
            }

            _aircraft.PublishAlert(
                $"REPAIR FAILED: {anomaly.AnomalyName} - {anomaly.GetPilotAction()}",
                AeroSimulator.Core.Aircraft.Enums.Severity.Medium);
            return false;
        }

        _aircraft.PublishAlert(
            "NO ACTIVE REPAIRABLE ANOMALY - standby checks completed",
            AeroSimulator.Core.Aircraft.Enums.Severity.Info);
        return false;
    }

    private bool IsAnomalyAlreadyActive(string typeCode)
    {
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