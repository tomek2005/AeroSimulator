namespace AeroSimulator.Core.Aircraft.Systems;

using System;

public class WingSystem : IAircraftSystem
{
    public double FlapsPosition { get; private set; }
    public bool SpoilersDeployed { get; private set; }
    public double IceAccumulation { get; private set; }
    
    public bool IsOffline { get; private set; }

    public void SetFlaps(double position)
    {
        if (IsOffline) return;
        FlapsPosition = Math.Clamp(position, 0.0, 1.0);
    }

    public void ToggleSpoilers()
    {
        if (IsOffline) return;
        SpoilersDeployed = !SpoilersDeployed;
    }

    public void AddIce(double amount)
    {
        IceAccumulation = Math.Min(1.0, IceAccumulation + amount);
    }

    public void RemoveIce(double amount)
    {
        IceAccumulation = Math.Max(0.0, IceAccumulation - amount);
    }

    public bool IsIceCritical() => IceAccumulation > 0.8;
    
    public void SetOffline()
    {
        IsOffline = true;
    }

    public bool Reboot()
    {
        IsOffline = false;
        IceAccumulation = 0.0;
        return true;
    }
}