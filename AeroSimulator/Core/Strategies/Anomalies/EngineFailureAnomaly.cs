using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;
using AeroSim.Core.Aircraft.Systems;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Complete engine failure (flame-out). Unlike <see cref="EngineFireAnomaly"/>,
/// this is a clean shutdown with no fire and no explosion — but the engine
/// cannot be restarted if health is below 20 % or altitude is too low.
/// The RPM sensor faults immediately (reads 0 even if the engine is later
/// restarted successfully, until the sensor is repaired).
/// </summary>
public sealed class EngineFailureAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double SingleEngineSpeedDecayPerSec = 0.5;   // kts/s when 1 engine lost
    private const double RestartMinAltitudeFt         = 5_000;

    // ─── State ─────────────────────────────────────────────────────────────────

    private readonly int _engineIndex;
    private bool _restartAttempted;

    // ─── Constructor ───────────────────────────────────────────────────────────

    /// <param name="engineIndex">0 = Engine 1, 1 = Engine 2.</param>
    public EngineFailureAnomaly(int engineIndex = 0)
    {
        _engineIndex = engineIndex;
    }

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => $"ENGINE {_engineIndex + 1} FAILURE";
    public override string   Description   => $"Engine {_engineIndex + 1} flame-out — thrust lost.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0003;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_engineIndex + 1} FLAME-OUT -- RPM decaying to zero !!";

    public override string GetPilotAction() =>
        "Press [R] to attempt in-flight engine restart. Maintain speed above stall.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        _restartAttempted = false;

        var engine = ctx.GetEngine(_engineIndex);
        engine.Stop();

        // RPM sensor faults — reads 0 even though it may come back later.
        var rpmSensor = _engineIndex == 0
            ? ctx.Sensors.Engine1RPM
            : ctx.Sensors.Engine2RPM;
        rpmSensor.ApplyDamage(0.7);   // pushes into Fault state

        ctx.Publish(new Events.SensorFaultEvent
        {
            Source     = AnomalyName,
            Level      = Severity.Medium,
            Message    = $"ENG{_engineIndex + 1}-RPM sensor FAULT after flame-out",
            SensorName = rpmSensor.SensorName,
            State      = rpmSensor.State
        });

        ctx.Publish(new Events.SystemFailureEvent
        {
            Source  = AnomalyName,
            Level   = Severity.Critical,
            Message = $"Engine {_engineIndex + 1} FAILED — flame-out, RPM decaying",
            System  = _engineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2,
            Health  = ctx.GetSystemHealth(_engineIndex == 0 ? SystemType.Engine1 : SystemType.Engine2)
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // If only one engine is running, aircraft gradually bleeds speed.
        bool otherEngineOk = ctx.GetEngine(_engineIndex == 0 ? 1 : 0).Health > 0.1;
        if (!otherEngineOk)
        {
            data.Speed = Math.Max(0, data.Speed - SingleEngineSpeedDecayPerSec * deltaT);
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        _restartAttempted = true;
        var data   = ctx.FlightData;
        var engine = ctx.GetEngine(_engineIndex);

        // Cannot restart too low or with heavily damaged engine.
        if (data.Altitude < RestartMinAltitudeFt)
        {
            PublishAlert(ctx, $"Engine {_engineIndex + 1} restart FAILED — altitude too low", Severity.High);
            return false;
        }

        bool restarted = engine.Restart();
        if (restarted)
        {
            // Sensor still faulted — reads 0 until manually repaired.
            PublishAlert(ctx,
                $"Engine {_engineIndex + 1} RESTARTED — RPM sensor still faulty, readings unreliable",
                Severity.Medium);
        }

        return restarted;
    }
}