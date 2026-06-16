using AeroSimulator.Controllers;
using AeroSimulator.Core.Aircraft.Enums;
using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class ResolveAnomalyCommand : IFlightCommand
{
    private readonly AnomalyEngine _anomalyEngine;

    public ResolveAnomalyCommand(AnomalyEngine anomalyEngine)
    {
        _anomalyEngine = anomalyEngine;
    }

    public string Name => "ResolveAnomaly";
    public string Details => "Attempted current anomaly resolution";

    public void Execute(AircraftModel aircraft)
    {
        if (_anomalyEngine.TryResolveActiveAnomaly())
        {
            return;
        }

        aircraft.FuelSystem.SealLeak();
        aircraft.HydraulicSystem.EmergencyGearExtension();
        aircraft.ElectricalSystem.SwitchToBackupBattery();

        for (int i = 0; i < aircraft.EngineCount; i++)
        {
            var engine = aircraft.GetEngine(i);
            if (engine.IsOnFire) engine.ExtinguishFire();
        }

        aircraft.PublishAlert(
            "MANUAL RECOVERY ACTIONS: leak sealed, gear extension commanded, backup battery selected, fire bottles checked",
            Severity.Info);
    }

    public void Undo(AircraftModel aircraft) { }
}
