using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

/// <summary>
/// Wing fire anomaly. Decays WingHealth in DamageModel at 1%/sec.
///   WingHealth ≤ 0.50 → DamageModel.AsymmetricDragActive = true
///   WingHealth ≤ 0.20 → asymmetric drift rate ramps up
///   WingHealth ≤ 0.00 → DamageModel.IsGameOver = true
/// </summary>
public sealed class WingFireAnomaly : AbstractAnomaly
{
    private const double HealthDecayPerSec         = 0.01;   // 1 %/s
    private const double AsymmetricDragThreshold   = 0.50;
    private const double ElectricalDamageThreshold = 0.20;
    private const double ElectricalDamageAmount    = 0.60;

    private bool _asymmetricDragTriggered;
    private bool _electricalDamageApplied;

    public override string   AnomalyName   => "WING STRUCTURAL FIRE";
    public override string   Description   => "Fire spread to wing structure — structural failure imminent.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0; // kaskada, nie losowane
    public override bool     CanBeResolved => false;

    public override string GetWarningMessage() =>
        "!! CRITICAL ALERT: WING STRUCTURAL FIRE -- structural failure imminent !!";

    public override string GetPilotAction() =>
        "Counteract asymmetric drift manually. Plan immediate emergency landing.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _asymmetricDragTriggered = false;
        _electricalDamageApplied = false;

        // Blokuje klapy i spoilery
        ctx.WingSystem.SetOffline();
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        // ── Odejmuj zdrowie skrzydła z DamageModel ──────────────────────────
        ctx.DamageModel.WingHealth =
            Math.Max(0.0, ctx.DamageModel.WingHealth - HealthDecayPerSec * deltaT);

        double wingHealth = ctx.DamageModel.WingHealth;

        // ── 50 % → asymetryczny opór ────────────────────────────────────────
        if (!_asymmetricDragTriggered && wingHealth <= AsymmetricDragThreshold)
        {
            _asymmetricDragTriggered             = true;
            ctx.DamageModel.AsymmetricDragActive = true;
        }

        // ── 20 % → okablowanie się pali, elektryka pada ─────────────────────
        if (!_electricalDamageApplied && wingHealth <= ElectricalDamageThreshold)
        {
            _electricalDamageApplied = true;
            ctx.ApplyDamage(SystemType.Electrical, ElectricalDamageAmount);
        }

        // ── 0 % → GAME OVER ─────────────────────────────────────────────────
        if (wingHealth <= 0.0)
        {
            ctx.DamageModel.TriggerGameOver("Wing structural failure — total loss of lift.");
            SelfResolve();
        }

        // Wizualne znoszenie boczne (efekt asymetrii)
        data.ApplyAsymmetricDrift(15.0 * data.AsymmetricDrag, deltaT);
    }

    protected override bool OnResolve(Aircraft ctx) => false;
}