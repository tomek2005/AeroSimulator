namespace AeroSimulator.Core.Strategies.Weather;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public class IcingConditionsStrategy : IWeatherStrategy
{
    public string Name => "ICING CONDITIONS";

    public void Apply(Aircraft aircraft, double dt)
    {
        aircraft.FlightData.TemperatureC = -15.0;
        aircraft.FlightData.WindSpeedKnots = 22.0;
    }
}