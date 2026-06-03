namespace AeroSimulator.Core.Aircraft.Systems;

public class AvionicsSystem
{
    public bool IsPowered { get; private set; } = true;
    public bool HasCriticalError { get; private set; }

    public void PowerOn() => IsPowered = true;
    public void PowerOff() => IsPowered = false;

    public void TriggerSoftwareGlitch()
    {
        if (IsPowered)
        {
            HasCriticalError = true;
        }
    }

    public bool Reboot()
    {
        if (!IsPowered) return false;
        
        HasCriticalError = false;
        return true;
    }
}