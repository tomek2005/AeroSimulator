namespace AeroSimulator.Core.Aircraft.Systems;

public class WeatherSystem
{
    public bool IsActive { get; private set; } = true;
    
    public double RadarRangeNm { get; set; } = 80.0;
    
    public void TurnOn() => IsActive = true;
    public void TurnOff() => IsActive = false;

    public bool DetectHazard(double hazardDistanceNm)
    {
        if (!IsActive) return false;
        return hazardDistanceNm <= RadarRangeNm;
    }
}