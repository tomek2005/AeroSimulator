using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;
using AeroSim.Core.Aircraft.Systems;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Wing fire anomaly — the most dangerous cascade in the simulation.
/// Wing health decays at 1 %/sec. At 50 % health asymmetric drag activates
/// and the aircraft begins drifting toward the burning side. At 20 % health
/// wiring burns out and the electrical system takes heavy damage. At 0 %
/// structural failure triggers immediate GAME OVER.
/// </summary>
public sealed class WingFireAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double HealthDecayPerSec          = 0.01;   // 1 %/s base melt rate
    private const double AsymmetricDragThreshold    = 0.50;
    private const double ElectricalDamageThreshold  = 0.20;
    private const double ElectricalDamageAmount     = 0.60;
    private const double ExtinguishMinHealth        = 0.40;   // can't suppress below 40 %

    // ─── State ─────────────────────────────────────────────────────────────────

    private bool _asymmetricDragTriggered;
    private bool _electricalDamageApplied;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "WING FIRE";
    public override string   Description   => "Wing is on fire — structural failure imminent.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0;   // cascade-only
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        "!! CRITICAL: WING FIRE DETECTED -- structural failure imminent !!";

    public override string GetPilotAction() =>
        "Press [R] for wing fire suppression -- ACT FAST";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        _asymmetricDragTriggered = false;
        _electricalDamageApplied = false;

        ctx.WingSystem.StartFire();

        ctx.Publish(new Events.WingFireEvent
        {
            Source  = AnomalyName,
            Level   = Severity.Critical,
            Message = "WING FIRE started — wing health decaying",
            Side    = ctx.DamageModel.AsymmetricDragSide
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // Advance fire state and melt the wing.
        ctx.WingSystem.Update(deltaT, ctx.DamageModel);
        double wingHealth = ctx.WingSystem.Health;

        // ── Threshold: 50 % → asymmetric drag ────────────────────────────────
        if (!_asymmetricDragTriggered && wingHealth <= AsymmetricDragThreshold)
        {
            _asymmetricDragTriggered = true;
            ctx.DamageModel.AsymmetricDragActive = true;

            ctx.Publish(new Events.AsymmetricDragEvent
            {
                Source      = AnomalyName,
                Level       = Severity.Critical,
                Message     = $"Wing health {wingHealth * 100:F0}% -- ASYMMETRIC DRAG ACTIVE",
                DamagedSide = ctx.DamageModel.AsymmetricDragSide,
                DriftRate   = ctx.DamageModel.DriftDegPerSec
            });
        }

        // ── Threshold: 20 % → electrical wiring burns ─────────────────────────
        if (!_electricalDamageApplied && wingHealth <= ElectricalDamageThreshold)
        {
            _electricalDamageApplied = true;
            ctx.ApplyDamage(SystemType.Electrical, ElectricalDamageAmount);

            ctx.Publish(new Events.SystemFailureEvent
            {
                Source  = AnomalyName,
                Level   = Severity.Critical,
                Message = "Wing wiring burned — ELECTRICAL SYSTEM damaged",
                System  = SystemType.Electrical,
                Health  = ctx.GetSystemHealth(SystemType.Electrical)
            });
        }

        // ── Threshold: 0 % → structural failure / GAME OVER ──────────────────
        if (wingHealth <= 0)
        {
            ctx.DamageModel.IsGameOver      = true;
            ctx.DamageModel.GameOverReason  = "Wing structural failure";

            ctx.Publish(new Events.GameOverEvent
            {
                Source  = AnomalyName,
                Level   = Severity.Critical,
                Message = "STRUCTURAL FAILURE — wing destroyed",
                Reason  = ctx.DamageModel.GameOverReason
            });

            SelfResolve();
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        // Cannot suppress a wing that is already mostly gone.
        if (ctx.WingSystem.Health < ExtinguishMinHealth)
            return false;

        return ctx.WingSystem.ExtinguishFire();
    }
}