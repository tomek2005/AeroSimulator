namespace AeroSimulator.Core.Aircraft.Systems;

public class AutopilotSystem
{
    public bool IsEngaged { get; private set; }
    public double TargetAltitude { get; private set; }

    public void Engage() => IsEngaged = true;
    public void Disengage() => IsEngaged = false;
    
    public void SetTargetAltitude(double altitude)
    {
        TargetAltitude = altitude;
    }

    public void ResyncAltitude(double altitude)
    {
        if (IsEngaged)
        {
            TargetAltitude = altitude;
        }
    }
}