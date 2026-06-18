namespace AeroSimulator.Core.Aircraft.Sensors;

// Capacitance fuel quantity sensor. Reads FlightData.FuelLevelKg.
public class FuelSensor : Sensor
{
    public FuelSensor() : base("FUEL-SNS")
    {
    }

    protected override double Scale => 0.05;
}