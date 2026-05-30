using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;

namespace AeroSim.Core.Strategies.Anomalies;

/// <summary>
/// Electrical system failure. Main bus drops immediately, taking the autopilot
/// offline and reducing all sensor accuracy by 40 %. After 30 seconds the
/// secondary bus also fails, knocking out the navigation system. Resolving via
/// backup battery partially restores sensors but at reduced fidelity (battery
/// power is weaker than main bus).
/// </summary>
public sealed class ElectricalFailureAnomaly : AbstractAnomaly
{
    // ─── Constants ─────────────────────────────────────────────────────────────

    private const double SensorAccuracyPenalty      = 0.40;
    private const double BackupSensorRecovery        = 0.20;   // weaker than full power
    private const double SecondaryBusFailureSec      = 30.0;
    private const double SystemDecayPerSec           = 0.005;  // unpowered systems decay slowly

    // ─── State ─────────────────────────────────────────────────────────────────

    private bool _secondaryBusFailed;
    private bool _onBackupBattery;

    // ─── IAnomaly ──────────────────────────────────────────────────────────────

    public override string   AnomalyName   => "ELECTRICAL FAILURE";
    public override string   Description   => "Main electrical bus failure — autopilot offline, sensors degraded.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0004;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        "!! WARNING: ELECTRICAL FAILURE -- autopilot offline, sensors degraded !!";

    public override string GetPilotAction() =>
        "Press [R] to switch to backup battery. Navigate manually — autopilot unavailable.";

    // ─── Template method implementations ──────────────────────────────────────

    protected override void OnTrigger(Aircraft.Aircraft ctx, FlightData data)
    {
        _secondaryBusFailed = false;
        _onBackupBattery    = false;

        ctx.ElectricalSystem.MainBusVoltage = 0;

        // Autopilot requires main bus power → goes offline immediately.
        ctx.AutopilotSystem.Disengage();

        // All sensors lose 40 % accuracy.
        foreach (var sensor in ctx.Sensors.GetAllSensors())
            sensor.ApplyDamage(SensorAccuracyPenalty);

        ctx.Publish(new Events.SystemFailureEvent
        {
            Source  = AnomalyName,
            Level   = Severity.High,
            Message = "ELECTRICAL MAIN BUS FAILED — autopilot offline, all sensors -40% accuracy",
            System  = SystemType.Electrical,
            Health  = ctx.GetSystemHealth(SystemType.Electrical)
        });
    }

    protected override void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        // After 30 s the secondary bus also fails, taking nav offline.
        if (!_secondaryBusFailed && _activeDuration >= SecondaryBusFailureSec)
        {
            _secondaryBusFailed = true;
            ctx.ElectricalSystem.SecondaryBusVoltage = 0;
            ctx.NavigationSystem.SetOffline();

            ctx.Publish(new Events.SystemFailureEvent
            {
                Source  = AnomalyName,
                Level   = Severity.Critical,
                Message = "SECONDARY BUS FAILED — NAVIGATION SYSTEM offline",
                System  = SystemType.Navigation,
                Health  = ctx.GetSystemHealth(SystemType.Navigation)
            });
        }

        // Unpowered systems slowly decay while electrical is down.
        if (!_onBackupBattery)
        {
            ctx.ApplyDamage(SystemType.Navigation,  SystemDecayPerSec * deltaT);
            ctx.ApplyDamage(SystemType.Autopilot,   SystemDecayPerSec * deltaT);
        }
    }

    protected override bool OnResolve(Aircraft.Aircraft ctx)
    {
        bool switched = ctx.ElectricalSystem.SwitchToBackupBattery();
        if (switched)
        {
            _onBackupBattery = true;

            // Partially recover sensor accuracy — battery voltage is lower than main bus.
            foreach (var sensor in ctx.Sensors.GetAllSensors())
                sensor.Repair();   // reset, then re-apply smaller penalty

            // Re-apply a smaller accuracy penalty to represent weak battery power.
            foreach (var sensor in ctx.Sensors.GetAllSensors())
                sensor.ApplyDamage(SensorAccuracyPenalty - BackupSensorRecovery);

            PublishAlert(ctx,
                "Backup battery active — sensors partially recovered, nav still offline",
                Severity.Medium);
        }
        return switched;
    }
}