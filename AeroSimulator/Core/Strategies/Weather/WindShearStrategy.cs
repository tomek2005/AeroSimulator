namespace AeroSimulator.Core.Strategies.Weather;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public class WindShearStrategy : IWeatherStrategy
{
    private readonly Random _rng = new();
    public string Name => "WIND SHEAR";

    public void Apply(Aircraft aircraft, double dt)
    {
        double shearModifier = (_rng.NextDouble() - 0.5) * 20.0;
        aircraft.FlightData.WindSpeedKnots = Math.Clamp(40.0 + shearModifier, 10.0, 70.0);
    }
}