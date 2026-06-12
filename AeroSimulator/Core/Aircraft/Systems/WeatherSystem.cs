namespace AeroSimulator.Core.Aircraft.Systems;

public class WeatherSystem : IAircraftSystem
{
    public bool IsActive { get; private set; } = true;
    public double RadarRangeNm { get; private set; } = 80.0;

    // Spełnienie kontraktu IAircraftSystem
    public bool IsOffline => !IsActive;

    public void TurnOn() => IsActive = true;
    public void TurnOff() => IsActive = false;

    public bool DetectHazard(double hazardDistanceNm)
    {
        if (!IsActive) return false;
        return hazardDistanceNm <= RadarRangeNm;
    }

    // Integracja z interfejsem awarii
    public void SetOffline() => TurnOff();
    
    public bool Reboot()
    {
        TurnOn();
        return true;
    }
}