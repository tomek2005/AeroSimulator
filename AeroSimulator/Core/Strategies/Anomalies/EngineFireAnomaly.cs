using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Events;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public sealed class EngineFireAnomaly : AbstractAnomaly
{
    private const double HealthDecayPerSec = 0.02;
    private const double WingSpreadChance = 0.30;
    private const double WingSpreadIntervalSec = 10.0;
    private const double TempSensorNoise = 0.20;

    private readonly int _engineIndex;
    private double _timeSinceSpreadRoll;
    private bool _wingFireTriggered;
    private bool _exploded;

    public EngineFireAnomaly(int engineIndex = 0)
    {
        _engineIndex = engineIndex;
    }

    public override string AnomalyName => $"ENGINE {_engineIndex + 1} FIRE";
    public override string Description => $"Engine {_engineIndex + 1} is on fire — suppression required immediately.";
    public override Severity Level => Severity.Critical;
    public override double Probability => 0.0;
    public override bool CanBeResolved => true;
    public int EngineIndex => _engineIndex;

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_engineIndex + 1} FIRE DETECTED -- activate suppression !!";

    public override string GetPilotAction() =>
        "Press [R] to activate engine fire suppression.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _timeSinceSpreadRoll = 0;
        _wingFireTriggered = false;
        _exploded = false;

        ctx.GetEngine(_engineIndex).StartFire();

        var tempSensor = ctx.Sensors.EngineTemps[_engineIndex];
        tempSensor.AddNoise(TempSensorNoise);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (_exploded) return;

        var engine = ctx.GetEngine(_engineIndex);
        
        engine.ApplyDamage(HealthDecayPerSec * deltaT);

        _timeSinceSpreadRoll += deltaT;
        if (!_wingFireTriggered && _timeSinceSpreadRoll >= WingSpreadIntervalSec)
        {
            _timeSinceSpreadRoll = 0;
            if (RollChance(WingSpreadChance))
            {
                _wingFireTriggered = true;
                
                ctx.Publish(new SystemFailureEvent(
                    "EngineFire",
                    1.0,
                    $"CASCADE:WING_FIRE:{_engineIndex}"));
            }
        }

        if (engine.Health <= 0 && !_exploded)
        {
            _exploded = true;
            engine.Explode(ctx, data);

            var rpmSensor = ctx.Sensors.EngineRPMs[_engineIndex];
            rpmSensor.Kill();

            SelfResolve();
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        bool success = ctx.GetEngine(_engineIndex).ExtinguishFire();
        if (success)
        {
            var tempSensor = ctx.Sensors.EngineTemps[_engineIndex];
            tempSensor.ClearNoise();
        }

        return success;
    }
}