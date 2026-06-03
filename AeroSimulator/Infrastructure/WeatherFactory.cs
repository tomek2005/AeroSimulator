namespace AeroSimulator.Infrastructure;

using System;

public class WeatherFactory
{
    private static readonly Random _random = new();

    public static (double WindSpeed, double WindDirection, bool Turbulence) GenerateDynamicWeather()
    {
        // Losowanie prędkości wiatru od 0 do 35 węzłów
        double windSpeed = _random.NextDouble() * 35.0;
        double windDirection = _random.Next(0, 360);
        bool turbulence = windSpeed > 20.0; // Silny wiatr generuje turbulencje

        return (windSpeed, windDirection, turbulence);
    }
}