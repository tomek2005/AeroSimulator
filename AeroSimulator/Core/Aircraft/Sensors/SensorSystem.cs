using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Aggregates every sensor on the aircraft.
/// Supports dynamic number of engines (1, 2, 4, 6, etc.).
/// The view and autopilot <b>always read from this class</b>, never from
/// <see cref="FlightData"/> directly.
/// </summary>
public class SensorSystem
{
    /// <summary>Barometric altimeter.</summary>
    public AltitudeSensor Altitude { get; } = new();

    /// <summary>Pitot-static airspeed indicator.</summary>
    public AirspeedSensor Airspeed { get; } = new();

    /// <summary>Fuel quantity.</summary>
    public FuelSensor FuelLevel { get; } = new();

    /// <summary>Hydraulic system pressure.</summary>
    public HydraulicSensor HydraulicPressure { get; } = new();

    // ── Dynamiczne tablice dla wielu silników ─────────────────────────────

    /// <summary>Tachometers for all engines (0-based index).</summary>
    public EngineSensor[] EngineRPMs { get; }

    /// <summary>Exhaust gas temperature sensors for all engines (0-based index).</summary>
    public EngineTempSensor[] EngineTemps { get; }

    // ── Konstruktor ───────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the sensor system with a specific number of engines.
    /// </summary>
    /// <param name="engineCount">The number of engines on this aircraft model.</param>
    public SensorSystem(int engineCount)
    {
        if (engineCount < 1)
            throw new ArgumentException("Aircraft must have at least 1 engine.", nameof(engineCount));

        EngineRPMs = new EngineSensor[engineCount];
        EngineTemps = new EngineTempSensor[engineCount];

        // Tworzymy sensory w pętli - używamy indeksu 'i' (0-based index)
        for (int i = 0; i < engineCount; i++)
        {
            EngineRPMs[i] = new EngineSensor(i);          // Wygeneruje nazwę: ENG1-SNS, ENG2-SNS...
            EngineTemps[i] = new EngineTempSensor(i); // Wygeneruje nazwę: ENG1-TMP, ENG2-TMP...
        }
    }

    // ── Cache i lista sensorów ───────────────────────────────────────────

    private readonly Dictionary<string, double> _cache = new();
    private IReadOnlyList<ISensor>? _allSensors;

    /// <summary>All sensor instances in display order.</summary>
    public IReadOnlyList<ISensor> GetAllSensors()
    {
        if (_allSensors == null)
        {
            var list = new List<ISensor>
            {
                Altitude,
                Airspeed,
                FuelLevel,
                HydraulicPressure
            };
            
            list.AddRange(EngineRPMs);
            list.AddRange(EngineTemps);
            
            _allSensors = list.AsReadOnly();
        }
        return _allSensors;
    }

    // ── Update ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads all sensors against the current true values from
    /// <paramref name="data"/> and caches the results.
    /// </summary>
    public void Update(double dt, FlightData data)
    {
        _cache[Altitude.SensorName] = Altitude.Read(data.Altitude);
        _cache[Airspeed.SensorName] = Airspeed.Read(data.Speed);
        _cache[FuelLevel.SensorName] = FuelLevel.Read(data.FuelLevelKg);

        // Pętla przechodzi przez każdy silnik, niezależnie czy jest ich 2, 4 czy 6!
        for (int i = 0; i < EngineRPMs.Length; i++)
        {
            _cache[EngineRPMs[i].SensorName] = EngineRPMs[i].Read(data.EngineRPMs[i]);
            _cache[EngineTemps[i].SensorName] = EngineTemps[i].Read(data.EngineTempsC[i]);
        }

        // HydraulicPressure is not in FlightData; the caller must supply a real value.
        if (!_cache.ContainsKey(HydraulicPressure.SensorName))
            _cache[HydraulicPressure.SensorName] = HydraulicPressure.Read(3000.0);
    }

    /// <summary>Updates the hydraulic pressure sensor specifically (called by HydraulicSystem).</summary>
    public void UpdateHydraulicReading(double realPressure)
    {
        _cache[HydraulicPressure.SensorName] = HydraulicPressure.Read(realPressure);
    }

    // ── Reszta metod (GetReading, AddNoise, DamageRandomSensor) pozostaje BEZ ZMIAN ─────────────────

    /// <summary>
    /// Returns the cached reading for the sensor with the given name.
    /// </summary>
    public double GetReading(string sensorName) =>
        _cache.TryGetValue(sensorName, out double value) ? value : -1.0;

    public void AddNoiseToAll(double amount)
    {
        foreach (ISensor sensor in GetAllSensors())
            sensor.AddNoise(amount);
    }

    public void ClearNoiseFromAll()
    {
        foreach (ISensor sensor in GetAllSensors())
            sensor.ClearNoise();
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
        GetAllSensors()
            .Where(s => s.State is SensorState.Fault or SensorState.Dead)
            .ToList()
            .AsReadOnly();

    public bool HasAnySensorFault() =>
        GetAllSensors().Any(s => s.State != SensorState.OK);
}