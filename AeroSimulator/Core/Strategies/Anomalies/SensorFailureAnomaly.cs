using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Aircraft.Sensors;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

// Random sensor failure anomaly. Preferentially targets altitude and airspeed
// sensors because those are coupled to the autopilot — a faulted altitude sensor
// causes the autopilot to drift the real altitude by ±50 ft/sec, forcing the
// player to disengage AP and fly manually until the sensor is repaired.
public sealed class SensorFailureAnomaly : AbstractAnomaly
{
    private const double SensorDamageAmount = 0.70;

    private ISensor? _targetSensor;
    private bool _isAltitudeSensor;
    private bool _autopilotWarningIssued;

    public override string AnomalyName => "SENSOR FAILURE";
    public override string Description => "Critical flight sensor has failed — instrument readings unreliable.";
    public override Severity Level => Severity.High;
    public override double Probability => 0.0006;
    public override bool CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: SENSOR FAULT on {_targetSensor?.SensorName ?? "UNKNOWN"} -- readings unreliable !!";

    public override string GetPilotAction() =>
        "Press [R] to attempt sensor recalibration. Disengage autopilot if altitude sensor is affected.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _autopilotWarningIssued = false;

        _targetSensor = RollChance(0.60)
            ? PickCriticalSensor(ctx)
            : ctx.Sensors.DamageRandomSensor();

        _isAltitudeSensor = _targetSensor == ctx.Sensors.Altitude;

        _targetSensor?.ApplyDamage(SensorDamageAmount);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (_targetSensor == null) return;
        
        if (_isAltitudeSensor && _targetSensor.State == SensorState.Fault && ctx.AutopilotSystem.IsEngaged)
        {
            if (!_autopilotWarningIssued)
            {
                _autopilotWarningIssued = true;
                PublishAlert(ctx, "AUTOPILOT receiving faulty telemetry -- DISENGAGE AP immediately",
                    Severity.Critical);
            }
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        if (_targetSensor == null) return false;

        _targetSensor.Repair();

        if (_isAltitudeSensor && ctx.AutopilotSystem.IsEngaged)
            ctx.AutopilotSystem.ResyncAltitude(ctx.FlightData.Altitude);

        return true;
    }

    private ISensor PickCriticalSensor(Aircraft ctx)
    {
        return new Random().NextDouble() < 0.5 ? ctx.Sensors.Altitude : ctx.Sensors.Airspeed;
    }
}