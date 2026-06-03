namespace AeroSimulator.Core.Aircraft.Systems;

public class NavigationSystem
{
    public bool IsOffline { get; private set; }

    public void SetOffline()
    {
        IsOffline = true;
    }

    public void Reboot()
    {
        IsOffline = false;
    }
}