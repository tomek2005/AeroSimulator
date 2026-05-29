namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Capacitance fuel quantity sensor. Reads <see cref="FlightData.FuelLevelKg"/>.
/// A fuel leak may cause it to read slightly high (fuel sloshing).
/// </summary>
public class FuelSensor : Sensor
{
    public FuelSensor() : base("FUEL-SNS") { }

    protected override double Scale => 0.05;    // very accurate when healthy
}