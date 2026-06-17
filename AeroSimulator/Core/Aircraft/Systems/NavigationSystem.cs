namespace AeroSimulator.Core.Aircraft.Systems;

public class NavigationSystem : IAircraftSystem
{
    public bool IsOffline { get; private set; }

    public void SetOffline() => IsOffline = true;

    public bool Reboot()
    {
        IsOffline = false;
        return true;
    }
}