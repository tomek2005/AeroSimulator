using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Hydraulic system failure. Pressure drops to zero, locking flaps and — if the
/// gear was mid-transit — jamming it in a half-retracted position. The hydraulic
/// pressure sensor immediately faults. Emergency gear extension remains available
/// as the resolution action, though flaps will stay stuck regardless.
/// </summary>
public sealed class HydraulicFailureAnomaly : AbstractAnomaly
{
    // ─── State ─────────────────────────────────────────────────────────────────

    private bool _gearWasMidTransit;
    private bool _landingWarningIssued;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "HYDRAULIC FAILURE";
    public override string   Description   => "Hydraulic system pressure lost — gear and flaps may be stuck.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0004;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        "!! WARNING: HYDRAULIC FAILURE -- gear/flap control lost !!";

    public override string GetPilotAction() =>
        "Press [R] to activate emergency gear extension. Flaps remain stuck.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        _landingWarningIssued = false;

        // Check if gear is in motion — if so, it jams.
        _gearWasMidTransit = ctx.HydraulicSystem.IsGearTransiting;

        ctx.HydraulicSystem.Pressure = 0;

        if (_gearWasMidTransit)
        {
            ctx.HydraulicSystem.GearJammed = true;
            PublishAlert(ctx, "Gear JAMMED in transit — emergency extension required", Severity.Critical);
        }

        // Fault the hydraulic pressure sensor.
        ctx.Sensors.HydraulicPressure.ApplyDamage(0.9);   // pushes to Fault/Dead
        ctx.Publish(new Events.SensorFaultEvent
        {
            Source     = AnomalyName,
            Level      = Severity.High,
            Message    = "HYD-PRESS sensor FAULT after hydraulic failure",
            SensorName = ctx.Sensors.HydraulicPressure.SensorName,
            State      = ctx.Sensors.HydraulicPressure.State
        });

        ctx.Publish(new Events.SystemFailureEvent
        {
            Source  = AnomalyName,
            Level   = Severity.High,
            Message = "HYDRAULIC SYSTEM FAILED — pressure = 0",
            System  = SystemType.Hydraulics,
            Health  = ctx.GetSystemHealth(SystemType.Hydraulics)
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // In LandingState with gear still jammed → escalate warning.
        if (ctx.HydraulicSystem.GearJammed
            && data.Altitude < 3_000
            && !_landingWarningIssued)
        {
            _landingWarningIssued = true;
            PublishAlert(ctx,
                "GEAR JAMMED — approach with caution, brace for hard landing",
                Severity.Critical);
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        // Emergency extension gets the gear down (gravity-drop), but flaps stay stuck.
        bool extended = ctx.HydraulicSystem.EmergencyGearExtension();
        if (extended)
        {
            ctx.HydraulicSystem.GearJammed = false;
            PublishAlert(ctx,
                "Emergency gear extension successful — flaps remain stuck at current position",
                Severity.Medium);
        }
        return extended;
    }
}