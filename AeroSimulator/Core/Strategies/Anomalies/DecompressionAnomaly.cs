using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Rapid explosive decompression. Only valid above 25 000 ft. Forces an
/// immediate emergency descent to 10 000 ft (safe altitude for breathing without
/// oxygen masks). Altitude and airspeed sensors get heavy noise from the pressure
/// equalisation blast. Cannot be resolved by the player — the only correct action
/// is descending below 10 000 ft. Failure to do so within 60 seconds triggers
/// pilot incapacitation and GAME OVER.
/// </summary>
public sealed class DecompressionAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double MinAltitudeFt          = 25_000;
    private const double SafeAltitudeFt         = 10_000;
    private const double PressureNoiseBoost     = 0.30;
    private const double IncapacitationTimeSec  = 60.0;

    // ─── State ─────────────────────────────────────────────────────────────────

    private bool _incapacitated;
    private bool _descentStarted;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "DECOMPRESSION";
    public override string   Description   => "Explosive decompression — emergency descent to 10 000 ft required.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0003;
    public override bool     CanBeResolved => false;

    public override string GetWarningMessage() =>
        "!! MAYDAY: DECOMPRESSION -- DESCEND IMMEDIATELY TO 10 000 FT !!";

    public override string GetPilotAction() =>
        "Press [X] repeatedly to descend below 10 000 ft. 60 seconds before incapacitation.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        // Guard: only valid at high altitude.
        if (data.Altitude < MinAltitudeFt) { SelfResolve(); return; }

        _incapacitated = false;
        _descentStarted = false;

        // Override the autopilot target altitude to force a dive.
        data.TargetAltitude = SafeAltitudeFt;
        ctx.AutopilotSystem.SetTargetAltitude(SafeAltitudeFt);

        // Pressure wave garbles altitude and speed sensors.
        ctx.Sensors.Altitude.AddNoise(PressureNoiseBoost);
        ctx.Sensors.Airspeed.AddNoise(PressureNoiseBoost);

        ctx.Publish(new Events.SensorFaultEvent
        {
            Source     = AnomalyName,
            Level      = Severity.Critical,
            Message    = "DECOMPRESSION: altitude and airspeed sensors heavily noised",
            SensorName = "ALT-SNS + SPD-SNS",
            State      = Aircraft.Sensors.SensorState.Noisy
        });

        ctx.Publish(new Events.MaydayEvent
        {
            Source         = AnomalyName,
            Level          = Severity.Critical,
            Message        = "MAYDAY MAYDAY — EXPLOSIVE DECOMPRESSION",
            Reason         = "Explosive decompression",
            EmergencyType  = EmergencyType.Decompression
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        if (_incapacitated) return;

        // Check if the player has started descending (target alt set low enough).
        if (data.Altitude < SafeAltitudeFt || data.TargetAltitude <= SafeAltitudeFt)
        {
            if (!_descentStarted)
            {
                _descentStarted = true;
                // Clear sensor noise once safely below pressurised threshold.
                if (data.Altitude < SafeAltitudeFt)
                {
                    ctx.Sensors.Altitude.ClearNoise();
                    ctx.Sensors.Airspeed.ClearNoise();
                    SelfResolve();
                }
            }
            return;
        }

        // 60-second countdown — pilot passes out if still at altitude.
        if (_activeDuration >= IncapacitationTimeSec)
        {
            _incapacitated               = true;
            ctx.DamageModel.IsGameOver   = true;
            ctx.DamageModel.GameOverReason = "Pilot incapacitation — failed to descend after decompression";

            ctx.Publish(new Events.GameOverEvent
            {
                Source  = AnomalyName,
                Level   = Severity.Critical,
                Message = "PILOT INCAPACITATED — failed to descend in time",
                Reason  = ctx.DamageModel.GameOverReason
            });
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        // CanBeResolved = false — this method is never called via player input.
        return false;
    }
}