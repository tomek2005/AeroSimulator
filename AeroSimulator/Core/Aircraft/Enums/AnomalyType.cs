namespace AeroSimulator.Core.Aircraft.Enums;

/// <summary>All anomaly types that can be spawned or cascade-triggered.</summary>
public enum AnomalyType
{
    EngineFailure,
    BirdStrike,
    EngineFire,
    WingFire,
    HydraulicFailure,
    FuelLeak,
    ElectricalFailure,
    Decompression,
    Turbulence,
    Icing,
    RunwayIncursion,
    Microburst,
    SensorFailure
}