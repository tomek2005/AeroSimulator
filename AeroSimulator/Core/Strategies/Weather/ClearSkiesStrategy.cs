namespace AeroSimulator.Core.Strategies.Weather;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;
public class ClearSkiesStrategy : IWeatherStrategy
{
    public string Name => "CLEAR SKIES";

    public void Apply(Aircraft aircraft, double dt)
    {
        aircraft.FlightData.WindSpeedKnots = 5.0;
        aircraft.FlightData.WindDirectionDeg = 270.0;
        aircraft.FlightData.TemperatureC = 15.0;
    }
}