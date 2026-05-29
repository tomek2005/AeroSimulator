namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Barometric / radio altimeter. Reads <see cref="FlightData.Altitude"/>.
/// Slightly tighter noise than default (altimeters are precise instruments).
/// </summary>
public class AltitudeSensor : Sensor
{
    public AltitudeSensor() : base("ALT-SNS") { }

    /// <summary>Altitude sensors are precise; scale noise to ±8 % of reading.</summary>
    protected override double Scale => 0.08;
}