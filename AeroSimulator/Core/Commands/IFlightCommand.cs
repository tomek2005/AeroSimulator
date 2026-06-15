using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public interface IFlightCommand
{
    string Name { get; }
    string Details { get; }
    void Execute(AircraftModel aircraft);
    void Undo(AircraftModel aircraft);
}
