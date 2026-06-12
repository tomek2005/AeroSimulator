namespace AeroSimulator.Core.Aircraft.Systems;

public interface IAircraftSystem
{
    bool IsOffline { get; }
    void SetOffline();
    bool Reboot();
}