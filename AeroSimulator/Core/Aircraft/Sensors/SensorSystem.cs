using System;
using System.Collections.Generic;
using System.Linq;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Common; // <-- IMPORT NASZEJ MONADY

namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Aggregates every sensor on the aircraft.
/// Supports dynamic number of engines (1, 2, 4, 6, etc.).
/// The view and autopilot <b>always read from this class</b>, never from
/// <see cref="FlightData"/> directly.
/// </summary>
public class SensorSystem
{
    public AltitudeSensor Altitude { get; } = new();
    public AirspeedSensor Airspeed { get; } = new();
    public FuelSensor FuelLevel { get; } = new();
    public HydraulicSensor HydraulicPressure { get; } = new();

    private readonly EngineSensor[] _engineRPMs;
    private readonly EngineTempSensor[] _engineTemps;

    public IReadOnlyList<EngineSensor> EngineRPMs => _engineRPMs;
    public IReadOnlyList<EngineTempSensor> EngineTemps => _engineTemps;

    public SensorSystem(int engineCount)
    {
        if (engineCount < 1)
            throw new ArgumentException("Aircraft must have at least 1 engine.", nameof(engineCount));

        _engineRPMs = new EngineSensor[engineCount];
        _engineTemps = new EngineTempSensor[engineCount];

        for (int i = 0; i < engineCount; i++)
        {
            _engineRPMs[i] = new EngineSensor(i);
            _engineTemps[i] = new EngineTempSensor(i);
        }
    }

    // 1. Słownik przechowuje teraz bezpieczne monady Option zamiast surowych liczb
    private readonly Dictionary<string, Option<double>> _cache = new();
    private IReadOnlyList<ISensor>? _allSensors;

    public IReadOnlyList<ISensor> GetAllSensors()
    {
        if (_allSensors == null)
        {
            var list = new List<ISensor>
            {
                Altitude, Airspeed, FuelLevel, HydraulicPressure
            };
            list.AddRange(_engineRPMs);
            list.AddRange(_engineTemps);
            _allSensors = list.AsReadOnly();
        }
        return _allSensors;
    }

    public void Update(double dt, FlightData data)
    {
        // Metoda Read() zwraca Option<double>, co idealnie wchodzi do nowego słownika
        _cache[Altitude.SensorName] = Altitude.Read(data.Altitude);
        _cache[Airspeed.SensorName] = Airspeed.Read(data.Speed);
        _cache[FuelLevel.SensorName] = FuelLevel.Read(data.FuelLevelKg);

        for (int i = 0; i < _engineRPMs.Length; i++)
        {
            _cache[_engineRPMs[i].SensorName] = _engineRPMs[i].Read(data.EngineRPMs[i]);
            _cache[_engineTemps[i].SensorName] = _engineTemps[i].Read(data.EngineTempsC[i]);
        }

        if (!_cache.ContainsKey(HydraulicPressure.SensorName))
            _cache[HydraulicPressure.SensorName] = HydraulicPressure.Read(3000.0);
    }

    public void UpdateHydraulicReading(double realPressure)
    {
        _cache[HydraulicPressure.SensorName] = HydraulicPressure.Read(realPressure);
    }

    // 2. Pobieranie danych ze słownika — jeśli czujnika w ogóle nie ma, też zwracamy None(), a nie -1.0
    public Option<double> GetReading(string sensorName) =>
        _cache.TryGetValue(sensorName, out var value) ? value : Option<double>.None();

    public void AddNoiseToAll(double amount)
    {
        foreach (ISensor sensor in GetAllSensors()) sensor.AddNoise(amount);
    }

    public void ClearNoiseFromAll()
    {
        foreach (ISensor sensor in GetAllSensors()) sensor.ClearNoise();
    }

    public ISensor DamageRandomSensor()
    {
        var rng = new Random();
        var sensors = GetAllSensors();
        var alive = sensors.Where(s => s.State != SensorState.Dead).ToList();
        var target = alive.Count > 0 ? alive[rng.Next(alive.Count)] : sensors[rng.Next(sensors.Count)];

        double damage = 0.5 + rng.NextDouble() * 0.3;
        target.ApplyDamage(damage);
        return target;
    }

    public IReadOnlyList<ISensor> GetFaultySensors() =>
        GetAllSensors().Where(s => s.State is SensorState.Fault or SensorState.Dead).ToList().AsReadOnly();

    public bool HasAnySensorFault() =>
        GetAllSensors().Any(s => s.State != SensorState.OK);
}