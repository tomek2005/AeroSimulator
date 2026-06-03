namespace AeroSimulator.Core.Weather;

public class EnvironmentState
{
    public WeatherCondition Condition { get; set; } = WeatherCondition.Clear;
    
    public double WindSpeedKts { get; set; } = 5.0;
    
    public int WindDirectionDeg { get; set; } = 270;
    
    public double TurbulenceIntensity { get; set; } = 0.0;
    public double TemperatureCelsius { get; set; } = 15.0;
}