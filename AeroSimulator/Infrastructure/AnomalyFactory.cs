using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Strategies.Anomalies;

namespace AeroSimulator.Infrastructure;

using System;

public static class AnomalyFactory
{
    /// Fabryka tworzy czyste instancje. Jeśli anomalia wymaga konkretnego indeksu komponentu, jest on jawnie przekazywany z poziomu kontrolera (silnika).
    public static IAnomaly CreateAnomaly(string baseType, int? targetComponentIndex = null)
    {
        return baseType.ToUpper() switch
        {
            "BIRD_STRIKE" => new BirdStrikeAnomaly(),
            "WING_FIRE" => new WingFireAnomaly(),
            "HYDRAULIC_FAILURE" => new HydraulicFailureAnomaly(),
            "FUEL_LEAK" => new FuelLeakAnomaly(),
            "ELECTRICAL_FAILURE" => new ElectricalFailureAnomaly(),
            "DECOMPRESSION" => new DecompressionAnomaly(),
            "TURBULENCE" => new TurbulenceAnomaly(),
            "ICING" => new IcingAnomaly(),
            "SENSOR_FAILURE" => new SensorFailureAnomaly(),
            "MICROBURST" => new MicroburstAnomaly(),
            "RUNWAY_INCURSION" => new RunwayIncursionAnomaly(),
            
            "ENGINE_FIRE" => new EngineFireAnomaly(targetComponentIndex ?? 0),
            "ENGINE_FAILURE" => new EngineFailureAnomaly(targetComponentIndex ?? 0),

            _ => throw new ArgumentException($"[Factory Error] Unknown anomaly type: {baseType}")
        };
    }
}