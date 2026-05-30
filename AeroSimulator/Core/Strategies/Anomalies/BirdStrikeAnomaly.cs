using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;
using AeroSim.Core.Aircraft.Systems;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Bird strike anomaly. Can only occur below 10 000 ft.
/// Damages a random engine by 30 %, creates a G-force spike, and has a
/// 40 % chance of cascading into an <see cref="EngineFireAnomaly"/>.
/// This is a one-shot event: it auto-resolves after 10 seconds of vibration.
/// </summary>
public sealed class BirdStrikeAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double EngineDamage       = 0.30;
    private const double GForceSpike        = 0.50;
    private const double CascadeFireChance  = 0.40;
    private const double VibrationGForce    = 0.15;
    private const double SensorDamageThreshold = 0.50; // engine health below which sensor takes damage
    private const double SensorDamageAmount = 0.40;
    private const double AutoResolveSec     = 10.0;

    // ─── State ─────────────────────────────────────────────────────────────────

    private int    _struckEngineIndex;    // 0 = Engine 1, 1 = Engine 2
    private bool   _cascadeRolled;
    private bool   _sensorDamageApplied;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "BIRD STRIKE";
    public override string   Description   => "Foreign object ingested by engine — structural damage imminent.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0008;   // rare — low-altitude only
    public override bool     CanBeResolved => false;    // one-shot event, auto-clears

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_struckEngineIndex + 1} BIRD STRIKE -- check RPM sensor !!";

    public override string GetPilotAction() =>
        "Monitor engine RPM and temperature. Prepare for engine fire procedure.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        // Only valid below 10 000 ft — AnomalyEngine checks spawn conditions,
        // but we guard here too for cascade-injected cases.
        if (data.Altitude > 10_000) { SelfResolve(); return; }

        // Pick a random engine to strike.
        _struckEngineIndex = _rng.Next(0, ctx.EngineCount);
        _cascadeRolled     = false;
        _sensorDamageApplied = false;

        // Damage the engine.
        ctx.ApplyDamage(
            _struckEngineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2,
            EngineDamage);

        // G-force spike from the impact.
        data.GForce += GForceSpike;

        ctx.Publish(new Events.SystemFailureEvent
        {
            Source  = AnomalyName,
            Level   = Severity.High,
            Message = $"Engine {_struckEngineIndex + 1} struck by bird — health -{EngineDamage * 100:F0}%",
            System  = _struckEngineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2,
            Health  = ctx.GetSystemHealth(_struckEngineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2)
        });

        // 40 % cascade roll → engine fire.
        if (RollChance(CascadeFireChance))
        {
            _cascadeRolled = true;
            TriggerCascade(ctx, new EngineFireAnomaly(_struckEngineIndex));
        }
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // Simulate vibration while the debris is still clearing.
        double vibration = ((_rng.NextDouble() - 0.5) * 2.0) * VibrationGForce;
        data.GForce = Math.Max(0.8, data.GForce + vibration);

        // If engine health dropped below threshold and we haven't damaged the
        // sensor yet, do so now (debris jams the sensor).
        double engineHealth = ctx.GetSystemHealth(
            _struckEngineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2);

        if (!_sensorDamageApplied && engineHealth < SensorDamageThreshold)
        {
            _sensorDamageApplied = true;
            var sensor = _struckEngineIndex == 0
                ? ctx.Sensors.Engine1RPM
                : ctx.Sensors.Engine2RPM;
            sensor.ApplyDamage(SensorDamageAmount);

            ctx.Publish(new Events.SensorFaultEvent
            {
                Source     = AnomalyName,
                Level      = Severity.Medium,
                Message    = $"ENG{_struckEngineIndex + 1}-RPM sensor degraded by bird-strike debris",
                SensorName = sensor.SensorName,
                State      = sensor.State
            });
        }

        // Auto-resolve after fixed duration — it's a single event, not ongoing.
        if (_activeDuration >= AutoResolveSec)
            SelfResolve();
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        // Cannot be pilot-resolved; OnUpdate handles auto-resolution.
        return false;
    }
}