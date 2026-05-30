using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;
using AeroSim.Core.Aircraft.Systems;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Engine fire anomaly. Causes continuous health decay on the affected engine
/// (-3 %/sec) and has a 30 % chance every 10 seconds of spreading to the wing
/// via <see cref="WingFireAnomaly"/>. If the engine health reaches zero while
/// on fire, <see cref="EngineSystem.Explode"/> is called, spiking G-force and
/// causing direct wing structural damage.
/// </summary>
public sealed class EngineFireAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double HealthDecayPerSec    = 0.03;   // 3 %/s engine burn rate
    private const double WingSpreadChance     = 0.30;   // 30 % roll per interval
    private const double WingSpreadIntervalSec = 10.0;
    private const double TempSensorNoiseBoost = 0.20;

    // ─── State ─────────────────────────────────────────────────────────────────

    private readonly int _engineIndex;      // 0 = Engine 1, 1 = Engine 2
    private double       _timeSinceSpreadRoll;
    private bool         _wingFireTriggered;
    private bool         _exploded;

    // ─── Constructor ───────────────────────────────────────────────────────────

    /// <param name="engineIndex">0-based engine index (0 = Engine 1, 1 = Engine 2).</param>
    public EngineFireAnomaly(int engineIndex = 0)
    {
        _engineIndex = engineIndex;
    }

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => $"ENGINE {_engineIndex + 1} FIRE";
    public override string   Description   => $"Engine {_engineIndex + 1} is on fire — suppression required immediately.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0;      // only spawned via cascade
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_engineIndex + 1} FIRE DETECTED -- activate suppression !!";

    public override string GetPilotAction() =>
        "Press [R] to activate engine fire suppression.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        _timeSinceSpreadRoll = 0;
        _wingFireTriggered   = false;
        _exploded            = false;

        // Tell the engine system it is on fire (starts its own fire state tracking).
        ctx.GetEngine(_engineIndex).StartFire();

        ctx.Publish(new Events.EngineFireEvent
        {
            Source       = AnomalyName,
            Level        = Severity.Critical,
            Message      = $"Engine {_engineIndex + 1} FIRE — suppression required",
            EngineNumber = _engineIndex + 1
        });

        // Make the temperature sensor noisy (heat warps the housing).
        var tempSensor = _engineIndex == 0
            ? ctx.Sensors.Engine1Temp
            : ctx.Sensors.Engine2Temp;
        tempSensor.AddNoise(TempSensorNoiseBoost);
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        if (_exploded) return;

        var engine = ctx.GetEngine(_engineIndex);

        // Continuous health decay.
        ctx.ApplyDamage(
            _engineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2,
            HealthDecayPerSec * deltaT);

        // Wing spread roll every 10 seconds.
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

        // Engine destroyed while on fire → explosion.
        if (engine.Health <= 0 && !_exploded)
        {
            _exploded = true;
            engine.Explode(ctx, data);

            ctx.Publish(new Events.EngineExplosionEvent
            {
                Source       = AnomalyName,
                Level        = Severity.Critical,
                Message      = $"ENGINE {_engineIndex + 1} EXPLODED",
                EngineNumber = _engineIndex + 1
            });

            // Kill the RPM sensor — explosion destroys it.
            var rpmSensor = _engineIndex == 0
                ? ctx.Sensors.Engine1RPM
                : ctx.Sensors.Engine2RPM;
            rpmSensor.Kill();

            ctx.Publish(new Events.SensorFaultEvent
            {
                Source     = AnomalyName,
                Level      = Severity.Critical,
                Message    = $"ENG{_engineIndex + 1}-RPM sensor destroyed in explosion",
                SensorName = rpmSensor.SensorName,
                State      = Aircraft.Sensors.SensorState.Dead
            });

            SelfResolve();  // fire is gone after explosion (engine gone)
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        bool success = ctx.GetEngine(_engineIndex).ExtinguishFire();
        if (success)
        {
            // Clear temperature sensor noise on successful suppression.
            var tempSensor = _engineIndex == 0
                ? ctx.Sensors.Engine1Temp
                : ctx.Sensors.Engine2Temp;
            tempSensor.ClearNoise();
        }
        return success;
    }
}