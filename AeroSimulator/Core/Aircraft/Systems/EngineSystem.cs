namespace AeroSimulator.Core.Aircraft.Systems;

using System;
using AeroSimulator.Core.Aircraft; // Zapewnia widoczność Aircraft i FlightData

// Menedżer zarządzający wszystkimi silnikami
public class EngineSystem
{
    public EngineUnit[] Engines { get; }
    
    public int EngineCount => Engines.Length;

    public EngineSystem(int engineCount)
    {
        engineCount = Math.Max(1, engineCount); 
        Engines = new EngineUnit[engineCount];
        
        for (int i = 0; i < engineCount; i++)
        {
            Engines[i] = new EngineUnit();
        }
    }

    public EngineUnit GetEngine(int index)
    {
        if (index < 0 || index >= EngineCount) return Engines[0];
        return Engines[index];
    }
}

// Reprezentacja fizycznego, pojedynczego silnika
public class EngineUnit
{
    public double Health { get; set; } = 1.0;
    public bool IsOnFire { get; private set; }

    public void StartFire() => IsOnFire = true;
    
    public bool ExtinguishFire()
    {
        if (!IsOnFire) return false;
        IsOnFire = false;
        return true;
    }

    public void Explode(Aircraft ctx, FlightData data)
    {
        Health = 0.0;
        IsOnFire = false;
        if (data != null) data.GForce += 2.0;
    }

    public void Stop() { Health = 0; }

    public bool Restart()
    {
        if (Health > 0.2) 
        {
            Health = 1.0;
            return true;
        }
        return false; 
    }
}