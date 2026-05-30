using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;
using AeroSim.Core.Aircraft.Sensors;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Random sensor failure anomaly. Preferentially targets altitude and airspeed
/// sensors because those are coupled to the autopilot — a faulted altitude sensor
/// causes the autopilot to hold the wrong altitude (±50 ft/sec drift), forcing
/// the player to disengage AP and fly manually until the sensor is repaired.
/// </summary>
public sealed class SensorFailureAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double SensorDamageAmount     = 0.70;
    private const double AutopilotAltDriftFtSec = 50.0;   // drift rate when AP reads wrong alt

    // ─── State ─────────────────────────────────────────────────────────────────

    private ISensor? _targetSensor;
    private bool     _isAltitudeSensor;
    private bool     _autopilotWarningIssued;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "SENSOR FAILURE";
    public override string   Description   => "Critical flight sensor has failed — instrument readings unreliable.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0006;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: SENSOR FAULT on {_targetSensor?.SensorName ?? "UNKNOWN"} -- readings unreliable !!";

    public override string GetPilotAction() =>
        "Press [R] to attempt sensor recalibration. Disengage autopilot if altitude sensor is affected.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        _autopilotWarningIssued = false;

        // 60 % chance to pick altitude/airspeed (higher danger), 40 % fully random.
        _targetSensor = RollChance(0.60)
            ? PickCriticalSensor(ctx)
            : ctx.Sensors.DamageRandomSensor();

        _isAltitudeSensor = _targetSensor == ctx.Sensors.Altitude;

        if (_targetSensor != null)
        {
            _targetSensor.ApplyDamage(SensorDamageAmount);

            ctx.Publish(new Events.SensorFaultEvent
            {
                Source     = AnomalyName,
                Level      = Severity.High,
                Message    = $"SENSOR FAILED: {_targetSensor.SensorName} — state: {_targetSensor.State}",
                SensorName = _targetSensor.SensorName,
                State      = _targetSensor.State
            });
        }
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        if (_targetSensor == null) return;

        // If the altitude sensor is faulted and autopilot is on, it locks onto
        // the wrong (stuck) altitude reading and drifts the real aircraft.
        if (_isAltitudeSensor
            && _targetSensor.State == SensorState.Fault
            && ctx.AutopilotSystem.IsEngaged)
        {
            double direction = _rng.NextDouble() > 0.5 ? 1.0 : -1.0;
            data.Altitude += direction * AutopilotAltDriftFtSec * deltaT;

            if (!_autopilotWarningIssued)
            {
                _autopilotWarningIssued = true;
                PublishAlert(ctx,
                    "AUTOPILOT reading faulty altitude sensor — DISENGAGE AP immediately",
                    Severity.Critical);
            }
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        if (_targetSensor == null) return false;

        _targetSensor.Repair();

        // Resync autopilot to the now-correct altitude reading.
        if (_isAltitudeSensor && ctx.AutopilotSystem.IsEngaged)
            ctx.AutopilotSystem.ResyncAltitude(ctx.FlightData.Altitude);

        return true;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private ISensor PickCriticalSensor(Aircraft.Aircraft ctx)
    {
        // Alternate between altitude and airspeed for variety.
        return _rng.NextDouble() < 0.5
            ? ctx.Sensors.Altitude
            : ctx.Sensors.Airspeed;
    }
}