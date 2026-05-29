using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Runway incursion anomaly. Only valid in LandingState below 500 ft AGL.
/// ATC reports another aircraft or vehicle on the runway. The player has 15
/// seconds to execute a go-around by pressing [H]. Failure to do so results
/// in a forced collision GAME OVER.
/// </summary>
public sealed class RunwayIncursionAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double MaxAltitudeFt       = 500;
    private const double GoAroundWindowSec   = 15.0;
    private const string[] IncursionTypes    =
    [
        "AIRCRAFT on runway",
        "VEHICLE on runway",
        "AIRCRAFT entering runway",
        "EMERGENCY VEHICLE crossing"
    ];

    // ─── State ─────────────────────────────────────────────────────────────────

    private string _incursionType = string.Empty;
    private bool   _goAroundExecuted;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "RUNWAY INCURSION";
    public override string   Description   => "Runway not clear — go-around required immediately.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0010;   // higher during landing phase
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! ATC: GO AROUND -- {_incursionType} !!";

    public override string GetPilotAction() =>
        "Press [H] for go-around immediately. 15 seconds before collision.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        // Guard: only valid on approach below 500 ft.
        if (data.Altitude > MaxAltitudeFt) { SelfResolve(); return; }

        _incursionType    = IncursionTypes[_rng.Next(IncursionTypes.Length)];
        _goAroundExecuted = false;

        ctx.Publish(new Events.MaydayEvent
        {
            Source         = AnomalyName,
            Level          = Severity.Critical,
            Message        = $"ATC: GO AROUND — {_incursionType}",
            Reason         = _incursionType,
            EmergencyType  = EmergencyType.RunwayIncursion
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        if (_goAroundExecuted) return;

        // Issue escalating warnings as countdown ticks.
        double remaining = GoAroundWindowSec - _activeDuration;

        if (remaining <= 10 && remaining > 9)
            PublishAlert(ctx, $"10 SECONDS — GO AROUND NOW — {_incursionType}", Severity.Critical);
        else if (remaining <= 5 && remaining > 4)
            PublishAlert(ctx, $"5 SECONDS — COLLISION IMMINENT — {_incursionType}", Severity.Critical);

        // Countdown expired → collision.
        if (_activeDuration >= GoAroundWindowSec)
        {
            ctx.DamageModel.IsGameOver     = true;
            ctx.DamageModel.GameOverReason = $"Runway collision — {_incursionType}";

            ctx.Publish(new Events.GameOverEvent
            {
                Source  = AnomalyName,
                Level   = Severity.Critical,
                Message = $"RUNWAY COLLISION — {_incursionType}",
                Reason  = ctx.DamageModel.GameOverReason
            });
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        // Player executes go-around.
        _goAroundExecuted = true;
        ctx.Abort();   // triggers go-around in LandingState
        SelfResolve();
        return true;
    }
}