namespace AeroSimulator.Core.Weather;

using System;

public class WeatherEngine
{
    private readonly Random _rng = new();
    
    // Aktualny stan pogody, do którego dostęp będzie miał samolot/kontroler
    public EnvironmentState Current { get; } = new EnvironmentState();

    /// <summary>
    /// Metoda wywoływana w głównej pętli czasu. Symuluje delikatne zmiany wiatru.
    /// </summary>
    public void Update(double deltaT)
    {
        // 10% szans w każdej sekundzie na drobną zmianę wiatru (tzw. Random Walk)
        if (_rng.NextDouble() < 0.1 * deltaT) 
        {
            // Zmiana prędkości o +/- 1 węzeł
            Current.WindSpeedKts += (_rng.NextDouble() - 0.5) * 2.0;
            Current.WindSpeedKts = Math.Clamp(Current.WindSpeedKts, 0.0, 150.0);

            // Delikatna zmiana kierunku
            Current.WindDirectionDeg += _rng.Next(-5, 6);
            if (Current.WindDirectionDeg < 0) Current.WindDirectionDeg += 360;
            if (Current.WindDirectionDeg >= 360) Current.WindDirectionDeg -= 360;
        }
    }

    /// <summary>
    /// Pozwala "Bogu" (czyli graczowi lub systemowi losującemu) nagle zmienić pogodę.
    /// </summary>
    public void SetCondition(WeatherCondition condition)
    {
        Current.Condition = condition;
        
        switch (condition)
        {
            case WeatherCondition.Clear:
                Current.TurbulenceIntensity = 0.0;
                break;
            case WeatherCondition.Rain:
                Current.TurbulenceIntensity = 0.1;
                break;
            case WeatherCondition.Thunderstorm:
                Current.TurbulenceIntensity = 0.5;
                Current.WindSpeedKts += 15; // Nagły podmuch
                break;
            case WeatherCondition.SevereStorm:
                Current.TurbulenceIntensity = 0.9;
                Current.WindSpeedKts += 35;
                break;
        }
    }
}