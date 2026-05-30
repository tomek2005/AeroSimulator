using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Airframe and pitot icing anomaly. Only valid below 0 °C with high humidity.
/// Ice accumulates on wings, raising the effective stall speed by 1 kt/min.
/// Ice on the pitot tube adds progressive noise to the airspeed sensor.
/// If stall speed rises more than 15 kts above baseline the anomaly escalates
/// to High severity. Resolved by activating electrical de-icing, which also
/// clears the airspeed sensor noise.
/// </summary>
public sealed class IcingAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double TempThresholdC          = 0.0;
    private const double StallSpeedIncreasePerSec = 1.0 / 60.0;   // 1 kt/min
    private const double EscalationThresholdKts  = 15.0;
    private const double PitotNoisePerSec        = 0.003;          // progressive
    private const double MaxPitotNoise           = 0.40;

    // ─── State ─────────────────────────────────────────────────────────────────

    private double _stallSpeedIncrease;   // kt above baseline
    private double _currentPitotNoise;
    private bool   _escalated;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "ICING";
    public override string   Description   => "Ice accumulation on airframe — stall speed rising, airspeed unreliable.";
    public override Severity Level         => _escalated ? Severity.High : Severity.Medium;
    public override double   Probability   => 0.0008;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: ICING -- stall speed +{_stallSpeedIncrease:F1} kts, pitot sensor noisy !!";

    public override string GetPilotAction() =>
        "Press [R] to activate de-icing system. Reduce speed cautiously.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        // Guard: only valid in icing conditions.
        if (data.TemperatureC >= TempThresholdC) { SelfResolve(); return; }

        _stallSpeedIncrease  = 0;
        _currentPitotNoise   = 0;
        _escalated           = false;

        ctx.Publish(new Events.SystemFailureEvent
        {
            Source  = AnomalyName,
            Level   = Severity.Medium,
            Message = "ICING detected — stall speed rising, monitor airspeed",
            System  = SystemType.Wing,
            Health  = ctx.GetSystemHealth(SystemType.Wing)
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // Stop accumulating if temperature rises above freezing (flew to warmer air).
        if (data.TemperatureC >= TempThresholdC) return;

        // Stall speed creep.
        _stallSpeedIncrease += StallSpeedIncreasePerSec * deltaT;
        ctx.FlightData.StallSpeedOffset = _stallSpeedIncrease;   // read by FlightData.IsStalling()

        // Progressive pitot noise.
        double newNoise = Math.Min(PitotNoisePerSec * deltaT, MaxPitotNoise - _currentPitotNoise);
        if (newNoise > 0)
        {
            _currentPitotNoise += newNoise;
            ctx.Sensors.Airspeed.AddNoise(newNoise);
        }

        // Escalate if stall speed has risen enough.
        if (!_escalated && _stallSpeedIncrease >= EscalationThresholdKts)
        {
            _escalated = true;
            PublishAlert(ctx,
                $"ICING SEVERE — stall speed +{_stallSpeedIncrease:F0} kts above normal, ACTIVATE DE-ICING",
                Severity.High);
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        bool deIced = ctx.ElectricalSystem.ActivateDeIcing();
        if (deIced)
        {
            // Restore stall speed and clear pitot noise.
            ctx.FlightData.StallSpeedOffset = 0;
            ctx.Sensors.Airspeed.ClearNoise();
            _stallSpeedIncrease = 0;
            _currentPitotNoise  = 0;
        }
        return deIced;
    }
}