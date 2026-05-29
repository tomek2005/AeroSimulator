namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Hydraulic pressure transducer.
/// Reads <see cref="Systems.HydraulicSystem.Pressure"/>.
/// </summary>
public class HydraulicSensor : Sensor
{
    public HydraulicSensor() : base("HYD-SNS") { }

    protected override double Scale => 0.06;
}
