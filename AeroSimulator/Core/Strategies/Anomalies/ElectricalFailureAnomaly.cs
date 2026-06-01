using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Electrical system failure. Main bus drops immediately, taking the autopilot
/// offline and reducing all sensor accuracy by 40 %. After 30 seconds the
/// secondary bus also fails, knocking out the navigation system.
/// </summary>
public sealed class ElectricalFailureAnomaly : AbstractAnomaly
{
    private const double SensorAccuracyPenalty   = 0.40;
    private const double BackupSensorRecovery    = 0.20;
    private const double SecondaryBusFailureSec  = 30.0;
    private const double SystemDecayPerSec       = 0.005;

    private bool _secondaryBusFailed;
    private bool _onBackupBattery;

    public override string   AnomalyName   => "ELECTRICAL FAILURE";
    public override string   Description   => "Main electrical bus failure — autopilot offline, sensors degraded.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0004;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        "!! WARNING: ELECTRICAL FAILURE -- autopilot offline, sensors degraded !!";

    public override string GetPilotAction() =>
        "Press [R] to switch to backup battery. Navigate manually — autopilot unavailable.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _secondaryBusFailed = false;
        _onBackupBattery    = false;

        ctx.ElectricalSystem.MainBusVoltage = 0;
        ctx.AutopilotSystem.Disengage();

        foreach (var sensor in ctx.Sensors.GetAllSensors())
            sensor.ApplyDamage(SensorAccuracyPenalty);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (!_secondaryBusFailed && _activeDuration >= SecondaryBusFailureSec)
        {
            _secondaryBusFailed = true;
            ctx.ElectricalSystem.SecondaryBusVoltage = 0;
            ctx.NavigationSystem.SetOffline();
        }

        if (!_onBackupBattery)
        {
            ctx.ApplyDamage(SystemType.Navigation, SystemDecayPerSec * deltaT);
            ctx.ApplyDamage(SystemType.Autopilot,  SystemDecayPerSec * deltaT);
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        bool switched = ctx.ElectricalSystem.SwitchToBackupBattery();
        if (switched)
        {
            _onBackupBattery = true;

            foreach (var sensor in ctx.Sensors.GetAllSensors())
                sensor.Repair();

            foreach (var sensor in ctx.Sensors.GetAllSensors())
                sensor.ApplyDamage(SensorAccuracyPenalty - BackupSensorRecovery);
        }
        return switched;
    }
}