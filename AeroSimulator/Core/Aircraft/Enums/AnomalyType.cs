namespace AeroSimulator.Core.Aircraft.Enums;

// All anomaly types that can be spawned or cascade-triggered.
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