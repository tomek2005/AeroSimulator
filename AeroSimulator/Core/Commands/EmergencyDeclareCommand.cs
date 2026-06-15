using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class EmergencyDeclareCommand : IFlightCommand
{
    public string Name => "EmergencyDeclare";
    public string Details => "Emergency declared";

    public void Execute(AircraftModel aircraft) => aircraft.DeclareEmergency();
    public void Undo(AircraftModel aircraft) { }
}
