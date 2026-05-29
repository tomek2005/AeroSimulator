namespace AeroSimulator.Core.Aircraft.Enums;

/// <summary>Operational state of a sensor.</summary>
public enum SensorState
{
    /// <summary>Sensor is reading accurately.</summary>
    OK,

    /// <summary>Sensor is adding random noise to readings (turbulence, weather).</summary>
    Noisy,

    /// <summary>Sensor is stuck on a stale or wrong value.</summary>
    Fault,

    /// <summary>Sensor is completely non-functional; returns -1.</summary>
    Dead
}