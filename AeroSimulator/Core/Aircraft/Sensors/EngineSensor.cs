namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Engine tachometer + EGT gauge for one engine.
/// Receives raw values from FlightData via <see cref="Read"/> and adds simulation noise.
/// Killed instantly when the engine explodes.
/// </summary>
public class EngineSensor : Sensor
{
    /// <summary>Zero-based index of the engine this sensor monitors.</summary>
    public int EngineIndex { get; }

    /// <param name="engineIndex">Zero-based engine index (0 for Engine 1, 1 for Engine 2, itd.).</param>
    public EngineSensor(int engineIndex)
        : base($"ENG{engineIndex + 1}-SNS")
    {
        if (engineIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(engineIndex), "Engine index cannot be negative.");

        EngineIndex = engineIndex;
    }

    /// <summary>Engine sensors have looser tolerances; ±18 % noise scale.</summary>
    protected override double Scale => 0.18;
}