namespace AeroSimulator.Core.Aircraft.Sensors;

// Receives raw values from FlightData and adds simulation noise.
public class EngineSensor : Sensor
{
    public int EngineIndex { get; }

    public EngineSensor(int engineIndex)
        : base($"ENG{engineIndex + 1}-SNS")
    {
        if (engineIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(engineIndex), "Engine index cannot be negative.");

        EngineIndex = engineIndex;
    }
    
    protected override double Scale => 0.18;
}