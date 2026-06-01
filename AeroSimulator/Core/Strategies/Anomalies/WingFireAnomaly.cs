using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Engine fire anomaly. Causes continuous health decay on the affected engine
/// (-3 %/sec) and has a 30 % chance every 10 seconds of spreading to the wing
/// via <see cref="WingFireAnomaly"/>. If engine health reaches zero while on fire,
/// <see cref="EngineSystem.Explode"/> is called, spiking G-force and damaging the wing.
/// </summary>
public sealed class EngineFireAnomaly : AbstractAnomaly
{
    private const double HealthDecayPerSec     = 0.03;
    private const double WingSpreadChance      = 0.30;
    private const double WingSpreadIntervalSec = 10.0;
    private const double TempSensorNoise       = 0.20;

    private readonly int _engineIndex;
    private double       _timeSinceSpreadRoll;
    private bool         _wingFireTriggered;
    private bool         _exploded;

    /// <param name="engineIndex">0 = Engine 1, 1 = Engine 2.</param>
    public EngineFireAnomaly(int engineIndex = 0)
    {
        _engineIndex = engineIndex;
    }

    public override string   AnomalyName   => $"ENGINE {_engineIndex + 1} FIRE";
    public override string   Description   => $"Engine {_engineIndex + 1} is on fire — suppression required immediately.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_engineIndex + 1} FIRE DETECTED -- activate suppression !!";

    public override string GetPilotAction() =>
        "Press [R] to activate engine fire suppression.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _timeSinceSpreadRoll = 0;
        _wingFireTriggered   = false;
        _exploded            = false;

        ctx.GetEngine(_engineIndex).StartFire();

        var tempSensor = _engineIndex == 0
            ? ctx.Sensors.Engine1Temp
            : ctx.Sensors.Engine2Temp;
        tempSensor.AddNoise(TempSensorNoise);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (_exploded) return;

        var engine = ctx.GetEngine(_engineIndex);

        ctx.ApplyDamage(
            _engineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2,
            HealthDecayPerSec * deltaT);

        _timeSinceSpreadRoll += deltaT;
        if (!_wingFireTriggered && _timeSinceSpreadRoll >= WingSpreadIntervalSec)
        {
            _timeSinceSpreadRoll = 0;
            if (RollChance(WingSpreadChance))
            {
                _wingFireTriggered = true;
                TriggerCascade(ctx, new WingFireAnomaly());
            }
        }

        if (engine.Health <= 0 && !_exploded)
        {
            _exploded = true;
            engine.Explode(ctx, data);

            var rpmSensor = _engineIndex == 0
                ? ctx.Sensors.Engine1RPM
                : ctx.Sensors.Engine2RPM;
            rpmSensor.Kill();

            SelfResolve();
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        bool success = ctx.GetEngine(_engineIndex).ExtinguishFire();
        if (success)
        {
            var tempSensor = _engineIndex == 0
                ? ctx.Sensors.Engine1Temp
                : ctx.Sensors.Engine2Temp;
            tempSensor.ClearNoise();
        }
        return success;
    }
}