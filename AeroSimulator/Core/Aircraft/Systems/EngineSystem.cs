namespace AeroSimulator.Core.Aircraft.Systems;

using System;
using AeroSimulator.Core.Aircraft; // Zapewnia widoczność Aircraft i FlightData

// Menedżer zarządzający wszystkimi silnikami
public class EngineSystem : IAircraftSystem
{
    public EngineUnit[] Engines { get; }
    public int EngineCount => Engines.Length;
    
    // Spełnienie kontraktu IAircraftSystem
    public bool IsOffline { get; private set; }

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

    // Jeśli cały system silników zostanie odcięty awaryjnie
    public void SetOffline()
    {
        IsOffline = true;
        foreach (var engine in Engines)
        {
            engine.Stop();
        }
    }

    public bool Reboot()
    {
        IsOffline = false;
        foreach (var engine in Engines)
        {
            engine.Restart();
        }
        return true;
    }
}

// Reprezentacja fizycznego, pojedynczego silnika
public class EngineUnit
{
    // Zamiana na private set — zasada "Tell, Don't Ask"
    public double Health { get; private set; } = 1.0;
    public bool IsOnFire { get; private set; }
    public bool IsRunning { get; private set; } = true;

    public void ApplyDamage(double amount)
    {
        Health = Math.Max(0.0, Health - amount);
    }

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
        if (data != null) 
        {
            // Poprawka: Wywołanie dedykowanej metody bodźca zamiast nadpisywania pola
            data.ApplyGForceSpike(2.0);
        }
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public bool Restart()
    {
        if (Health > 0.2) 
        {
            IsRunning = true;
            return true;
        }
        return false; 
    }
}
