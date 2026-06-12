namespace AeroSimulator.Core.Strategies.Weather;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;
public class FogStrategy : IWeatherStrategy
{
    public string Name => "FOG";

    public void Apply(Aircraft aircraft, double dt)
    {
        aircraft.FlightData.WindSpeedKnots = 3.0;
        aircraft.FlightData.TemperatureC = 8.0;
        // Wpływa bezpośrednio na widoczność renderowaną w widgetach
    }
}