using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Microburst wind-shear anomaly. Only valid during approach (below 2 500 ft).
/// Creates a sudden +40 kt headwind immediately followed by a -40 kt tailwind.
/// Player must apply full throttle and pitch up within 5 seconds or CFIT occurs.
/// </summary>
public sealed class MicroburstAnomaly : AbstractAnomaly
{
    private const double MaxAltitudeFt       = 2_500;
    private const double HeadwindKts         = 40.0;
    private const double TailwindKts         = 40.0;
    private const double WindReversalSec     = 10.0;
    private const double ExtraDescentFtMin   = 1_000;
    private const double RecoveryWindowSec   = 5.0;
    private const double RecoveryThrottleMin = 0.90;
    private const double RecoveryPitchMinDeg = 5.0;

    private bool   _inTailwindPhase;
    private bool   _playerRecovered;
    private double _timeWithoutRecovery;

    public override string   AnomalyName   => "MICROBURST";
    public override string   Description   => "Microburst wind-shear on approach — full thrust required NOW.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0006;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        "!! CRITICAL: MICROBURST -- FULL THRUST, PITCH UP NOW !!";

    public override string GetPilotAction() =>
        "Press [W] for full throttle and maintain pitch up. 5 seconds to recover.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        if (data.Altitude > MaxAltitudeFt) { SelfResolve(); return; }

        _inTailwindPhase     = false;
        _playerRecovered     = false;
        _timeWithoutRecovery = 0;

        data.WindSpeedKnots   = HeadwindKts;
        data.WindDirectionDeg = data.Heading;
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (_playerRecovered) return;

        if (!_inTailwindPhase && _activeDuration >= WindReversalSec)
        {
            _inTailwindPhase      = true;
            data.WindSpeedKnots   = TailwindKts;
            data.WindDirectionDeg = (data.Heading + 180) % 360;

            PublishAlert(ctx,
                "WIND REVERSAL -- tailwind now, severe energy loss -- FULL THRUST",
                Severity.Critical);
        }

        double extraDescentFtSec = ExtraDescentFtMin / 60.0;
        data.Altitude      -= extraDescentFtSec * deltaT;
        data.VerticalSpeed  = Math.Min(data.VerticalSpeed, -ExtraDescentFtMin);

        bool throttleOk = data.Throttle >= RecoveryThrottleMin;
        bool pitchOk    = data.PitchAngleDeg >= RecoveryPitchMinDeg;

        if (throttleOk && pitchOk)
        {
            _playerRecovered = true;
            SelfResolve();
            return;
        }

        _timeWithoutRecovery += deltaT;
        if (_timeWithoutRecovery >= RecoveryWindowSec)
        {
            ctx.DamageModel.IsGameOver     = true;
            ctx.DamageModel.GameOverReason = "CFIT — microburst, failed to recover";
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        // Shortcut: pressing [R] applies full throttle
        ctx.FlightData.Throttle = 1.0;
        return false;
    }
}