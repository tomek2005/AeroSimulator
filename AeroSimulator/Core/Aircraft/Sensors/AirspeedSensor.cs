namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Pitot-static airspeed indicator. Reads <see cref="FlightData.Speed"/>.
/// Susceptible to icing — <see cref="Sensor.AddNoise"/> is called by the icing anomaly.
/// </summary>
public class AirspeedSensor : Sensor
{
    public AirspeedSensor() : base("SPD-SNS") { }

    /// <summary>Pitot tube is moderately sensitive; ±12 % noise scale.</summary>
    protected override double Scale => 0.12;
}
