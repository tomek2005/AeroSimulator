using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Fuel leak anomaly. Starts a leak at 80–250 kg/h. The fuel sensor reads
/// slightly higher than reality (fuel sloshing confuses the sensor). Every 60
/// seconds unresolved, a 20 % ignition risk check fires — if it triggers, the
/// leak cascades into an <see cref="EngineFireAnomaly"/> on the nearest engine.
/// </summary>
public sealed class FuelLeakAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double MinLeakRateKgH      = 80.0;
    private const double MaxLeakRateKgH      = 250.0;
    private const double FuelSensorNoiseBoost = 0.12;   // reads higher than reality
    private const double IgnitionCheckSec    = 60.0;
    private const double IgnitionChance      = 0.20;

    // ─── State ─────────────────────────────────────────────────────────────────

    private double _leakRateKgH;
    private double _timeSinceIgnitionCheck;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "FUEL LEAK";
    public override string   Description   => "Fuel system breach detected — loss of fuel and fire risk.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0005;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: FUEL LEAK detected -- {_leakRateKgH:F0} kg/h loss rate !!";

    public override string GetPilotAction() =>
        "Press [R] to seal fuel leak. Monitor fuel level closely.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        _leakRateKgH             = MinLeakRateKgH + _rng.NextDouble() * (MaxLeakRateKgH - MinLeakRateKgH);
        _timeSinceIgnitionCheck  = 0;

        ctx.FuelSystem.StartLeak(_leakRateKgH);

        // Fuel sensor becomes noisy — slosh makes it over-report.
        ctx.Sensors.FuelLevel.AddNoise(FuelSensorNoiseBoost);

        ctx.Publish(new Events.SystemFailureEvent
        {
            Source  = AnomalyName,
            Level   = Severity.High,
            Message = $"FUEL LEAK: {_leakRateKgH:F0} kg/h — seal immediately",
            System  = SystemType.Fuel,
            Health  = ctx.GetSystemHealth(SystemType.Fuel)
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // Leak is handled by FuelSystem.Update — we only track ignition risk here.
        _timeSinceIgnitionCheck += deltaT;

        if (_timeSinceIgnitionCheck >= IgnitionCheckSec)
        {
            _timeSinceIgnitionCheck = 0;

            if (ctx.FuelSystem.CheckIgnitionRisk() && RollChance(IgnitionChance))
            {
                // Cascade: fuel ignites → engine fire on a random engine.
                int engineIdx = _rng.Next(0, ctx.EngineCount);
                TriggerCascade(ctx, new EngineFireAnomaly(engineIdx));

                PublishAlert(ctx,
                    "FUEL IGNITION — engine fire cascade triggered",
                    Severity.Critical);
            }
        }

        // Escalate alert severity if fuel drops critically low.
        if (data.FuelRemainingPercent() < 5.0)
        {
            ctx.Publish(new Events.FuelCriticalEvent
            {
                Source           = AnomalyName,
                Level            = Severity.Critical,
                Message          = "FUEL CRITICAL — immediate landing required",
                RemainingPercent = data.FuelRemainingPercent()
            });
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        bool sealed_ = ctx.FuelSystem.SealLeak();
        if (sealed_)
            ctx.Sensors.FuelLevel.ClearNoise();
        return sealed_;
    }
}