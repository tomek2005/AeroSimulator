namespace AeroSimulator.Core.Aircraft.Sensors;

// Hydraulic pressure transducer. Reads Systems.HydraulicSystem.Pressure.

public class HydraulicSensor : Sensor
{
    public HydraulicSensor() : base("HYD-SNS")
    {
    }

    protected override double Scale => 0.06;
}