using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Runway incursion anomaly. Only valid in LandingState below 500 ft AGL.
/// ATC reports another aircraft or vehicle on the runway. The player has 15
/// seconds to execute a go-around by pressing [H]. Failure → collision GAME OVER.
/// </summary>
public sealed class RunwayIncursionAnomaly : AbstractAnomaly
{
    private const double MaxAltitudeFt     = 500;
    private const double GoAroundWindowSec = 15.0;

    private static readonly string[] IncursionTypes =
    [
        "AIRCRAFT on runway",
        "VEHICLE on runway",
        "AIRCRAFT entering runway",
        "EMERGENCY VEHICLE crossing"
    ];

    private string _incursionType = string.Empty;
    private bool   _goAroundExecuted;

    public override string   AnomalyName   => "RUNWAY INCURSION";
    public override string   Description   => "Runway not clear — go-around required immediately.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0010;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! ATC: GO AROUND -- {_incursionType} !!";

    public override string GetPilotAction() =>
        "Press [H] for go-around immediately. 15 seconds before collision.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        if (data.Altitude > MaxAltitudeFt) { SelfResolve(); return; }

        _incursionType    = IncursionTypes[_rng.Next(IncursionTypes.Length)];
        _goAroundExecuted = false;
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (_goAroundExecuted) return;

        double remaining = GoAroundWindowSec - _activeDuration;

        if (remaining <= 10 && remaining > 9)
            PublishAlert(ctx, $"10 SECONDS -- GO AROUND NOW -- {_incursionType}", Severity.Critical);
        else if (remaining <= 5 && remaining > 4)
            PublishAlert(ctx, $"5 SECONDS -- COLLISION IMMINENT -- {_incursionType}", Severity.Critical);

        if (_activeDuration >= GoAroundWindowSec)
        {
            ctx.DamageModel.IsGameOver     = true;
            ctx.DamageModel.GameOverReason = $"Runway collision — {_incursionType}";
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        _goAroundExecuted = true;
        ctx.Abort();
        SelfResolve();
        return true;
    }
}