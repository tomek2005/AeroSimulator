using AeroSimulator.Controllers;
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
    }

    public void Undo(AircraftModel aircraft) { }
}
