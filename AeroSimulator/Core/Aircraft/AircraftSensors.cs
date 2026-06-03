namespace AeroSimulator.Core.Aircraft;

using System;
using System.Collections.Generic;
using System.Linq;
using AeroSimulator.Core.Aircraft.Enums; 

public interface ISensor
{
    string SensorName { get; }
    SensorState State { get; }
    void ApplyDamage(double amount);
    void Repair();
    void AddNoise(double amount);
    void ClearNoise();
}

public class AircraftSensors
{
    private readonly Random _rng = new();

    // Nowe, dynamiczne tablice czujników dla dowolnej liczby silników
    public FlightSensor[] EngineRPM { get; }
    public FlightSensor[] EngineTemp { get; }
    
    public FlightSensor Altitude { get; } = new FlightSensor("Altitude");
    public FlightSensor Airspeed { get; } = new FlightSensor("Airspeed");
    public FlightSensor FuelLevel { get; } = new FlightSensor("Fuel Level");
    public FlightSensor HydraulicPressure { get; } = new FlightSensor("Hydraulic Pressure");

    // Konstruktor inicjujący odpowiednią liczbę czujników na bazie ilości silników
    public AircraftSensors(int engineCount)
    {
        engineCount = Math.Max(1, engineCount); // Zabezpieczenie (minimum 1 silnik)
        EngineRPM = new FlightSensor[engineCount];
        EngineTemp = new FlightSensor[engineCount];
        
        for (int i = 0; i < engineCount; i++)
        {
            EngineRPM[i] = new FlightSensor($"Engine {i + 1} RPM");
            EngineTemp[i] = new FlightSensor($"Engine {i + 1} Temp");
        }
    }

    public ISensor[] GetAllSensors()
    {
        var sensors = new List<ISensor> { Altitude, Airspeed, FuelLevel, HydraulicPressure };
        sensors.AddRange(EngineRPM);
        sensors.AddRange(EngineTemp);
        return sensors.ToArray();
    }

    public ISensor DamageRandomSensor()
    {
        var all = GetAllSensors();
        var target = all[_rng.Next(all.Length)];
        target.ApplyDamage(0.8);
        return target;
    }

    public void AddNoiseToAll(double amount)
    {
        foreach (var s in GetAllSensors()) s.AddNoise(amount);
    }

    public void ClearAllNoise()
    {
        foreach (var s in GetAllSensors()) s.ClearNoise();
    }
}

public class FlightSensor : ISensor
{
    public string SensorName { get; } 
    
    public SensorState State 
    {
        get
        {
            if (DamageLevel >= 1.0) return SensorState.Dead;
            if (IsFaulty && DamageLevel <= 0.1) return SensorState.Noisy; 
            if (IsFaulty) return SensorState.Fault;
            return SensorState.OK; 
        }
    }
    
    public bool IsFaulty { get; private set; }
    public double DamageLevel { get; private set; }

    public FlightSensor(string name) 
    { 
        SensorName = name; 
    }

    public void ApplyDamage(double amount)
    {
        DamageLevel = Math.Clamp(DamageLevel + amount, 0.0, 1.0);
        if (DamageLevel > 0.1) IsFaulty = true;
    }

    public void Repair()
    {
        DamageLevel = 0;
        IsFaulty = false;
    }

    public void AddNoise(double amount) => IsFaulty = true;
    
    public void ClearNoise()
    {
        if (DamageLevel <= 0.1) IsFaulty = false;
    }
    
    public void Kill() 
    { 
        DamageLevel = 1.0; 
        IsFaulty = true; 
    }
}