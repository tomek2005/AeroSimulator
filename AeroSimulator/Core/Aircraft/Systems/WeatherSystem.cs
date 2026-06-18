namespace AeroSimulator.Core.Aircraft.Systems;

public class WeatherSystem : IAircraftSystem
{
    public bool IsActive { get; private set; } = true;
    public double RadarRangeNm { get; private set; } = 80.0;
    public string CurrentCondition { get; private set; } = "CLEAR SKIES";
    public double SecondsToNextChange { get; private set; }
    
    public bool IsOffline => !IsActive;

    public void TurnOn() => IsActive = true;
    public void TurnOff() => IsActive = false;

    public bool DetectHazard(double hazardDistanceNm)
    {
        if (!IsActive) return false;
        return hazardDistanceNm <= RadarRangeNm;
    }

    public void SetCurrentCondition(string condition, double secondsToNextChange)
    {
        CurrentCondition = condition;
        SecondsToNextChange = Math.Max(0.0, secondsToNextChange);
    }
    
    public void SetOffline() => TurnOff();

    public bool Reboot()
    {
        TurnOn();
        return true;
    }
}