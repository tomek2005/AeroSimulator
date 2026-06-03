namespace AeroSimulator.Core.Aircraft.Systems;

using System;

public class WingSystem
{
    public double FlapsPosition { get; private set; } 
    
    public bool SpoilersDeployed { get; private set; }
    
    public double IceAccumulation { get; private set; }

    public void SetFlaps(double position)
    {
        FlapsPosition = Math.Clamp(position, 0.0, 1.0);
    }

    public void ToggleSpoilers()
    {
        SpoilersDeployed = !SpoilersDeployed;
    }

    public void AddIce(double amount)
    {
        IceAccumulation += amount;
    }

    public void RemoveIce(double amount)
    {
        IceAccumulation = Math.Max(0.0, IceAccumulation - amount);
    }

    public bool IsIceCritical() => IceAccumulation > 0.8;
}