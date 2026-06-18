namespace AeroSimulator.Core.Aircraft.Sensors;

// Reads engine temp value and if faulty add noise
public class EngineTempSensor : Sensor
{
    public int EngineIndex { get; }
    
    public EngineTempSensor(int engineIndex)
        : base($"ENG{engineIndex + 1}-TMP")
    {
        if (engineIndex < 0) throw new ArgumentOutOfRangeException(nameof(engineIndex));
        EngineIndex = engineIndex;
    }
    
    protected override double Scale => 0.15;
}