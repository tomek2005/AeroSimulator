using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;
/// <summary>
/// Hydraulic system failure. Pressure drops to zero, locking flaps and — if the
/// gear was mid-transit — jamming it in a half-retracted position.
/// Emergency gear extension remains available as the resolution action.
/// </summary>
public sealed class HydraulicFailureAnomaly : AbstractAnomaly
{
    private bool _landingWarningIssued;

    public override string   AnomalyName   => "HYDRAULIC FAILURE";
    public override string   Description   => "Hydraulic system pressure lost — gear and flaps may be stuck.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0004;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        "!! WARNING: HYDRAULIC FAILURE -- gear/flap control lost !!";

    public override string GetPilotAction() =>
        "Press [R] to activate emergency gear extension. Flaps remain stuck.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _landingWarningIssued = false;

        bool gearWasMidTransit = ctx.HydraulicSystem.IsGearTransiting;

        ctx.HydraulicSystem.Pressure = 0;

        if (gearWasMidTransit)
            ctx.HydraulicSystem.GearJammed = true;

        ctx.Sensors.HydraulicPressure.ApplyDamage(0.9);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (ctx.HydraulicSystem.GearJammed
            && data.Altitude < 3_000
            && !_landingWarningIssued)
        {
            _landingWarningIssued = true;
            PublishAlert(ctx,
                "GEAR JAMMED -- approach with caution, brace for hard landing",
                Severity.Critical);
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        bool extended = ctx.HydraulicSystem.EmergencyGearExtension();
        if (extended)
            ctx.HydraulicSystem.GearJammed = false;
        return extended;
    }
}