using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Microburst wind-shear anomaly. Only valid during approach (below 2 500 ft).
/// A violent downburst creates a sudden +40 kt headwind immediately followed
/// by a -40 kt tailwind — the classic microburst energy reversal. The aircraft
/// loses 1 000 ft/min extra descent rate during the shear. Player must apply
/// full throttle and pitch up within 5 seconds or the loss of lift causes a
/// controlled-flight-into-terrain GAME OVER.
/// </summary>
public sealed class MicroburstAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double MaxAltitudeFt         = 2_500;
    private const double HeadwindKts           = 40.0;
    private const double TailwindKts           = 40.0;
    private const double WindReversalSec       = 10.0;   // headwind → tailwind transition
    private const double ExtraDescentFtMin     = 1_000;
    private const double RecoveryWindowSec     = 5.0;
    private const double RecoveryThrottleMin   = 0.90;   // player must push throttle ≥ 90 %
    private const double RecoveryPitchMinDeg   = 5.0;

    // ─── State ─────────────────────────────────────────────────────────────────

    private bool   _inTailwindPhase;
    private bool   _playerRecovered;
    private double _timeWithoutRecovery;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "MICROBURST";
    public override string   Description   => "Microburst wind-shear on approach — full thrust required NOW.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0006;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        "!! CRITICAL: MICROBURST -- FULL THRUST, PITCH UP NOW !!";

    public override string GetPilotAction() =>
        "Press [W] for full throttle and maintain pitch up. 5 seconds to recover.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        // Guard: only during approach.
        if (data.Altitude > MaxAltitudeFt) { SelfResolve(); return; }

        _inTailwindPhase     = false;
        _playerRecovered     = false;
        _timeWithoutRecovery = 0;

        // Initial headwind — briefly increases lift then reverses.
        data.WindSpeedKnots    = HeadwindKts;
        data.WindDirectionDeg  = data.Heading;   // straight headwind

        ctx.Publish(new Events.SystemFailureEvent
        {
            Source  = AnomalyName,
            Level   = Severity.Critical,
            Message = "MICROBURST: severe downburst — +40kt headwind now, tailwind imminent",
            System  = SystemType.Navigation,
            Health  = 1.0
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        if (_playerRecovered) return;

        // Wind reversal after 10 seconds.
        if (!_inTailwindPhase && _activeDuration >= WindReversalSec)
        {
            _inTailwindPhase       = true;
            data.WindSpeedKnots    = TailwindKts;
            data.WindDirectionDeg  = (data.Heading + 180) % 360;   // direct tailwind

            PublishAlert(ctx,
                "WIND REVERSAL — tailwind now, severe energy loss — FULL THRUST",
                Severity.Critical);
        }

        // Extra forced descent during the shear.
        double extraDescentFtSec = ExtraDescentFtMin / 60.0;
        data.Altitude     -= extraDescentFtSec * deltaT;
        data.VerticalSpeed = Math.Min(data.VerticalSpeed, -ExtraDescentFtMin);

        // Check recovery: player must be at high throttle and positive pitch.
        bool throttleOk = data.Throttle >= RecoveryThrottleMin;
        bool pitchOk    = data.PitchAngleDeg >= RecoveryPitchMinDeg;

        if (throttleOk && pitchOk)
        {
            _playerRecovered = true;
            SelfResolve();
            PublishAlert(ctx, "Microburst recovery successful — continue go-around", Severity.Medium);
            return;
        }

        // Countdown to CFIT.
        _timeWithoutRecovery += deltaT;
        if (_timeWithoutRecovery >= RecoveryWindowSec)
        {
            ctx.DamageModel.IsGameOver     = true;
            ctx.DamageModel.GameOverReason = "CFIT — microburst, failed to recover";

            ctx.Publish(new Events.GameOverEvent
            {
                Source  = AnomalyName,
                Level   = Severity.Critical,
                Message = "CONTROLLED FLIGHT INTO TERRAIN — microburst",
                Reason  = ctx.DamageModel.GameOverReason
            });
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        // Resolved automatically when throttle + pitch conditions are met in OnUpdate.
        // Player action: push throttle [W] and maintain nose-up pitch.
        // This explicit call sets full throttle as a shortcut for [R].
        ctx.FlightData.Throttle = 1.0;
        return false;   // actual resolution detected in OnUpdate
    }
}