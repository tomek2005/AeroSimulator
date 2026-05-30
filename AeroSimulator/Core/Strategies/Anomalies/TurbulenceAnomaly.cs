using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Turbulence anomaly. Shakes the aircraft (altitude, speed, G-force oscillations)
/// and — crucially — degrades sensor accuracy while active. Medium or higher
/// severity adds noise to ALL sensors. Critical severity randomly damages a sensor
/// permanently. Auto-resolves after 3–8 minutes; player can accelerate resolution
/// by changing altitude ±2 000 ft.
/// </summary>
public sealed class TurbulenceAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double AltOscillationFt      = 200;
    private const double SpeedOscillationKts    = 15;
    private const double GForceOscillation      = 0.30;
    private const double SensorNoiseAmount      = 0.15;
    private const double MinDurationSec         = 180;   // 3 min
    private const double MaxDurationSec         = 480;   // 8 min

    // ─── State ─────────────────────────────────────────────────────────────────

    private Severity _turbulenceSeverity;
    private double   _totalDuration;       // randomised on trigger
    private bool     _sensorNoiseApplied;
    private bool     _criticalSensorDamaged;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "TURBULENCE";
    public override string   Description   => "Severe atmospheric turbulence — sensor readings may be unreliable.";
    public override Severity Level         => _turbulenceSeverity;
    public override double   Probability   => 0.0015;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: {_turbulenceSeverity.ToString().ToUpper()} TURBULENCE -- sensor noise active !!";

    public override string GetPilotAction() =>
        "Change altitude ±2000 ft with [Z]/[X] to exit turbulence layer.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        // Randomise severity each occurrence.
        _turbulenceSeverity     = (Severity)_rng.Next(1, 5);   // Low=1 … Critical=4
        _totalDuration          = MinDurationSec + _rng.NextDouble() * (MaxDurationSec - MinDurationSec);
        _sensorNoiseApplied     = false;
        _criticalSensorDamaged  = false;

        // Medium or higher → add noise to all sensors immediately.
        if (_turbulenceSeverity >= Severity.Medium)
        {
            _sensorNoiseApplied = true;
            ctx.Sensors.AddNoiseToAll(SensorNoiseAmount);

            ctx.Publish(new Events.SensorFaultEvent
            {
                Source     = AnomalyName,
                Level      = Severity.Medium,
                Message    = "SENSOR NOISE added to all sensors (turbulence)",
                SensorName = "ALL",
                State      = Aircraft.Sensors.SensorState.Noisy
            });
        }

        // Critical → additionally damage a random sensor permanently.
        if (_turbulenceSeverity == Severity.Critical && !_criticalSensorDamaged)
        {
            _criticalSensorDamaged = true;
            var damaged = ctx.Sensors.DamageRandomSensor();

            ctx.Publish(new Events.SensorFaultEvent
            {
                Source     = AnomalyName,
                Level      = Severity.Critical,
                Message    = $"CRITICAL TURBULENCE damaged sensor: {damaged.SensorName}",
                SensorName = damaged.SensorName,
                State      = damaged.State
            });
        }
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // Physical shaking: altitude, speed, G-force oscillate randomly.
        data.Altitude      += (_rng.NextDouble() - 0.5) * 2 * AltOscillationFt  * deltaT;
        data.Speed         += (_rng.NextDouble() - 0.5) * 2 * SpeedOscillationKts * deltaT;
        data.GForce        += (_rng.NextDouble() - 0.5) * 2 * GForceOscillation  * deltaT;
        data.GForce         = Math.Clamp(data.GForce, 0.2, 4.0);

        // Gentle pitch/roll shake.
        data.PitchAngleDeg += (_rng.NextDouble() - 0.5) * 4 * deltaT;
        data.RollAngleDeg  += (_rng.NextDouble() - 0.5) * 6 * deltaT;

        // Auto-resolve when duration expires.
        if (_activeDuration >= _totalDuration)
            OnResolve(ctx);
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        // Clear sensor noise boost — accuracy-based noise remains.
        if (_sensorNoiseApplied)
            ctx.Sensors.ClearAllNoise();

        SelfResolve();
        return true;
    }
}