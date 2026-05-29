namespace AeroSimulator.Core.Aircraft;



//Do sprawdzenia potem




/// <summary>
/// Mutable record of all live flight telemetry values.
/// This is the ground truth — systems write here, sensors read
/// from here and may return noisy or incorrect values to the view.
/// </summary>
public class FlightData
{
    // ── Position & attitude ──────────────────────────────────────────────
    public double Altitude { get; set; }
    public double Speed { get; set; }
    public double VerticalSpeed { get; set; }
    public double Heading { get; set; }
    public double PitchAngleDeg { get; set; }
    public double RollAngleDeg { get; set; }
    public double GForce { get; set; } = 1.0;

    // ── Autopilot targets ─────────────────────────────────────────────────
    public double TargetHeading { get; set; }
    public double TargetAltitude { get; set; }
    public double TargetSpeed { get; set; }

    // ── Map position ──────────────────────────────────────────────────────
    public double MapX { get; set; }
    public double MapY { get; set; }

    // ── Engines ───────────────────────────────────────────────────────────
    public double Throttle { get; set; }

    /// <summary>True RPM as a percentage of rated speed for all engines.</summary>
    public double[] EngineRPMs { get; set; }

    /// <summary>Exhaust gas temperature in degrees Celsius for all engines.</summary>
    public double[] EngineTempsC { get; set; }

    // ── Fuel ──────────────────────────────────────────────────────────────
    public double FuelLevelKg { get; set; }
    public double FuelFlowKgPerH { get; set; }
    public double FuelCapacityKg { get; set; }

    // ── Atmosphere ────────────────────────────────────────────────────────
    public double WindSpeedKnots { get; set; }
    public double WindDirectionDeg { get; set; }
    public double AirPressureHPa { get; set; } = 1013.25;
    public double TemperatureC { get; set; }

    // ── Timing ────────────────────────────────────────────────────────────
    public TimeSpan FlightTime { get; set; } = TimeSpan.Zero;

    // ── Damage / handling ─────────────────────────────────────────────────
    public double AsymmetricDrag { get; set; }

    // ── Config reference ──────────────────────────────────────────────────
    public AircraftConfig? Config { get; set; }

    // ── Konstruktor (NOWOŚĆ) ─────────────────────────────────────────────
    
    /// <summary>
    /// Initializes flight data with arrays for the specified number of engines.
    /// </summary>
    public FlightData(int engineCount)
    {
        if (engineCount < 1)
            throw new ArgumentException("Must have at least 1 engine.", nameof(engineCount));

        EngineRPMs = new double[engineCount];
        EngineTempsC = new double[engineCount];
        Reset(); // Ustawia domyślne temperatury itp.
    }

    // ── Derived calculations ──────────────────────────────────────────────
    public double FuelRemainingPercent() =>
        FuelCapacityKg > 0 ? FuelLevelKg / FuelCapacityKg * 100.0 : 0.0;

    public double EstimatedRangeKm()
    {
        if (FuelFlowKgPerH <= 0) return 0;
        double hoursRemaining = FuelLevelKg / FuelFlowKgPerH;
        return hoursRemaining * Speed * 1.852;
    }

    public bool IsStalling()
    {
        if (Config is null) return false;
        return Speed < Config.StallSpeedFlaps && Altitude > 0;
    }

    public bool IsOverspeed()
    {
        if (Config is null) return false;
        return Speed > Config.MaxSpeedKts;
    }

    public FlightDataSnapshot Snapshot() => new(this);

    /// <summary>
    /// Serialises key fields to a CSV row for black-box telemetry logging.
    /// Dynamicznie dodaje wszystkie silniki do formatu CSV.
    /// </summary>
    public string ToTelemetryString()
    {
        // Generujemy string z obrotami wszystkich silników po przecinku
        string engineRpmsCsv = string.Join(",", EngineRPMs.Select(rpm => rpm.ToString("F1")));

        return string.Join(",",
            DateTime.UtcNow.ToString("HH:mm:ss"),
            Altitude.ToString("F0"),
            Speed.ToString("F1"),
            VerticalSpeed.ToString("F0"),
            Heading.ToString("F1"),
            Throttle.ToString("F2"),
            engineRpmsCsv, // Tu wpadają obroty (np. "85.0,85.0,85.0,85.0")
            FuelLevelKg.ToString("F0"),
            GForce.ToString("F2"),
            PitchAngleDeg.ToString("F1"),
            RollAngleDeg.ToString("F1"));
    }

    public void Reset()
    {
        Altitude       = 0;
        Speed          = 0;
        VerticalSpeed  = 0;
        Heading        = 360;
        PitchAngleDeg  = 0;
        RollAngleDeg   = 0;
        GForce         = 1.0;
        Throttle       = 0;
        FuelFlowKgPerH = 0;
        FlightTime     = TimeSpan.Zero;
        AsymmetricDrag = 0;
        MapX           = 0;
        MapY           = 0;

        // Resetowanie tablic silników
        for (int i = 0; i < EngineRPMs.Length; i++)
        {
            EngineRPMs[i] = 0;
            EngineTempsC[i] = 15; // Domyślna temperatura
        }
    }

    public void ApplyAsymmetricDrift(double driftDegPerSec, double dt)
    {
        Heading = (Heading + driftDegPerSec * dt + 360.0) % 360.0;
    }
}

// ============================================================
//  FlightDataSnapshot — immutable point-in-time copy
// ============================================================

public record FlightDataSnapshot
{
    public DateTime Timestamp { get; init; }

    public double Altitude       { get; init; }
    public double Speed          { get; init; }
    public double VerticalSpeed  { get; init; }
    public double Heading        { get; init; }
    public double PitchAngleDeg  { get; init; }
    public double RollAngleDeg   { get; init; }
    public double GForce         { get; init; }
    public double Throttle       { get; init; }
    public double[] EngineRPMs   { get; init; } // Zmiana na tablicę
    public double[] EngineTempsC { get; init; } // Zmiana na tablicę
    public double FuelLevelKg    { get; init; }
    public double FuelFlowKgPerH { get; init; }
    public double WindSpeedKnots { get; init; }
    public double AsymmetricDrag { get; init; }
    public TimeSpan FlightTime   { get; init; }

    public FlightDataSnapshot(FlightData src)
    {
        Timestamp      = DateTime.UtcNow;
        Altitude       = src.Altitude;
        Speed          = src.Speed;
        VerticalSpeed  = src.VerticalSpeed;
        Heading        = src.Heading;
        PitchAngleDeg  = src.PitchAngleDeg;
        RollAngleDeg   = src.RollAngleDeg;
        GForce         = src.GForce;
        Throttle       = src.Throttle;
        
        // BARDZO WAŻNE: Klonujemy tablice! Bez .ToArray() snapshot
        // patrzyłby na te same dane co żywy symulator.
        EngineRPMs     = src.EngineRPMs.ToArray();
        EngineTempsC   = src.EngineTempsC.ToArray();
        
        FuelLevelKg    = src.FuelLevelKg;
        FuelFlowKgPerH = src.FuelFlowKgPerH;
        WindSpeedKnots = src.WindSpeedKnots;
        AsymmetricDrag = src.AsymmetricDrag;
        FlightTime     = src.FlightTime;
    }

    public string ToCsvRow()
    {
        string engineRpmsCsv = string.Join(",", EngineRPMs.Select(rpm => rpm.ToString("F1")));

        return string.Join(",",
            Timestamp.ToString("HH:mm:ss"),
            Altitude.ToString("F0"),
            Speed.ToString("F1"),
            VerticalSpeed.ToString("F0"),
            Heading.ToString("F1"),
            Throttle.ToString("F2"),
            engineRpmsCsv,
            FuelLevelKg.ToString("F0"),
            GForce.ToString("F2"),
            PitchAngleDeg.ToString("F1"),
            RollAngleDeg.ToString("F1"));
    }
}