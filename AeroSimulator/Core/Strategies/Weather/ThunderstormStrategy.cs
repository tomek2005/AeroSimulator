namespace AeroSimulator.Core.Strategies.Weather;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public class ThunderstormStrategy : IWeatherStrategy
{
    public string Name => "THUNDERSTORM";

    public void Apply(Aircraft aircraft, double dt)
    {
        aircraft.FlightData.WindSpeedKnots = 45.0;
        aircraft.FlightData.WindDirectionDeg = 180.0;
        
        // Zgodnie z sekcją 3.1: "Turbulence -> sensor noise on altimeter"
        // Wstrzykujemy szum losowy do systemów sensorów
        aircraft.Sensors.Altitude.AddNoise(0.15);
        aircraft.Sensors.Airspeed.AddNoise(0.10);
    }
}