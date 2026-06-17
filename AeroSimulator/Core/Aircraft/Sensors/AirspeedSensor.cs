namespace AeroSimulator.Core.Aircraft.Sensors;

// Pitot-static airspeed indicator. Reads FlightData.Speed.
public class AirspeedSensor : Sensor
{
    public AirspeedSensor() : base("SPD-SNS")
    {
    }
    
    protected override double Scale => 0.12;
}