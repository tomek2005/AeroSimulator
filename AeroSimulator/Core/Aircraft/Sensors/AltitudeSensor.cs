namespace AeroSimulator.Core.Aircraft.Sensors;

// Barometric / radio altimeter. Reads "FlightData.Altitude".
public class AltitudeSensor : Sensor
{
    public AltitudeSensor() : base("ALT-SNS")
    {
    }
    
    protected override double Scale => 0.08;
}