namespace AeroSimulator.Core.Strategies.Weather;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;
public class CrosswindStrategy : IWeatherStrategy
{
    public string Name => "CROSSWIND";

    public void Apply(Aircraft aircraft, double dt)
    {
        // Ustala silny wiatr boczny (prostopadły do aktualnego kursu samolotu)
        aircraft.FlightData.WindSpeedKnots = 35.0;
        aircraft.FlightData.WindDirectionDeg = (aircraft.FlightData.Heading + 90.0) % 360.0;
    }
}