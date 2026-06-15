using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class GoAroundCommand : IFlightCommand
{
    public string Name => "GoAround";
    public string Details => "Abort current phase / go around";

    public void Execute(AircraftModel aircraft) => aircraft.Abort();
    public void Undo(AircraftModel aircraft) { }
}
