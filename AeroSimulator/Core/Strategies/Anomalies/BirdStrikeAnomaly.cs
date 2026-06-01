using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Bird strike anomaly. Can only occur below 10 000 ft.
/// Damages a random engine by 30 %, creates a G-force spike, and has a
/// 40 % chance of cascading into an <see cref="EngineFireAnomaly"/>.
/// This is a one-shot event: it auto-resolves after 10 seconds of vibration.
/// </summary>
public sealed class BirdStrikeAnomaly : AbstractAnomaly
{
    private const double EngineDamage            = 0.30;
    private const double GForceSpike             = 0.50;
    private const double CascadeFireChance       = 0.40;
    private const double VibrationGForce         = 0.15;
    private const double SensorDamageThreshold   = 0.50;
    private const double SensorDamageAmount      = 0.40;
    private const double AutoResolveSec          = 10.0;

    private int  _struckEngineIndex;
    private bool _sensorDamageApplied;

    public override string   AnomalyName   => "BIRD STRIKE";
    public override string   Description   => "Foreign object ingested by engine — structural damage imminent.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0008;
    public override bool     CanBeResolved => false;

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_struckEngineIndex + 1} BIRD STRIKE -- check RPM sensor !!";

    public override string GetPilotAction() =>
        "Monitor engine RPM and temperature. Prepare for engine fire procedure.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        if (data.Altitude > 10_000) { SelfResolve(); return; }

        _struckEngineIndex   = _rng.Next(0, ctx.EngineCount);
        _sensorDamageApplied = false;

        ctx.ApplyDamage(
            _struckEngineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2,
            EngineDamage);

        data.GForce += GForceSpike;

        if (RollChance(CascadeFireChance))
            TriggerCascade(ctx, new EngineFireAnomaly(_struckEngineIndex));
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        double vibration = (_rng.NextDouble() - 0.5) * 2.0 * VibrationGForce;
        data.GForce = Math.Max(0.8, data.GForce + vibration);

        double engineHealth = ctx.GetSystemHealth(
            _struckEngineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2);

        if (!_sensorDamageApplied && engineHealth < SensorDamageThreshold)
        {
            _sensorDamageApplied = true;
            var sensor = _struckEngineIndex == 0
                ? ctx.Sensors.Engine1RPM
                : ctx.Sensors.Engine2RPM;
            sensor.ApplyDamage(SensorDamageAmount);
        }

        if (_activeDuration >= AutoResolveSec)
            SelfResolve();
    }

    protected override bool OnResolve(Aircraft ctx) => false;
}