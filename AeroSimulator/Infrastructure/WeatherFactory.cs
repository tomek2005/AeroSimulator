using AeroSimulator.Core.Strategies.Weather;

namespace AeroSimulator.Infrastructure;

using System;

public static class WeatherFactory
{
    public static IWeatherStrategy CreateWeather(string type)
    {
        return type.ToUpper() switch
        {
            "CLEAR" => new ClearSkiesStrategy(),
            "THUNDERSTORM" or "STORM" => new ThunderstormStrategy(),
            "FOG" => new FogStrategy(),
            "CROSSWIND" => new CrosswindStrategy(),
            "ICING" => new IcingConditionsStrategy(),
            "WINDSHEAR" => new WindShearStrategy(),
            _ => throw new ArgumentException($"[Error] Nieznany typ pogody: {type}")
        };
    }
}