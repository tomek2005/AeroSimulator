namespace AeroSimulator.Core.Aircraft.Sensors;

public class EngineTempSensor : Sensor
{
    /// <summary>Zero-based index of the engine this sensor monitors.</summary>
    public int EngineIndex { get; }

    /// <param name="engineIndex">Zero-based engine index (0 for Engine 1, 1 for Engine 2, itd.).</param>
    public EngineTempSensor(int engineIndex)
        : base($"ENG{engineIndex + 1}-TMP")
    {
        if (engineIndex < 0) throw new ArgumentOutOfRangeException(nameof(engineIndex));
        EngineIndex = engineIndex;
    }
    
    /// <summary>Engine temperature sensors have moderate tolerances; ±15 % noise scale.</summary>
    protected override double Scale => 0.15; 
}