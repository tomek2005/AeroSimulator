namespace AeroSimulator.Core.Aircraft.Systems;

public class FuelSystem : IAircraftSystem
{
    public double CurrentLeakRate { get; private set; }
    public bool IsLeaking => CurrentLeakRate > 0;
    public bool IsOffline { get; private set; }

    public void StartLeak(double rateKgH)
    {
        CurrentLeakRate = rateKgH;
    }

    public bool SealLeak()
    {
        if (IsLeaking)
        {
            CurrentLeakRate = 0;
            return true;
        }

        return false;
    }

    public bool CheckIgnitionRisk()
    {
        return CurrentLeakRate > 150.0;
    }

    public void SetOffline() => IsOffline = true;

    public bool Reboot()
    {
        IsOffline = false;
        return true;
    }
}