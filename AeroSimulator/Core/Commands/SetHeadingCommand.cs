using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class SetHeadingCommand : IFlightCommand
{
    private readonly double _rollDeltaDeg;
    private double _previousRoll;

    public SetHeadingCommand(double rollDeltaDeg)
    {
        _rollDeltaDeg = rollDeltaDeg;
    }

    public string Name => "SetHeading";
    public string Details => _rollDeltaDeg >= 0 ? "Roll right" : "Roll left";

    public void Execute(AircraftModel aircraft)
    {
        _previousRoll = aircraft.FlightData.RollAngleDeg;
        aircraft.FlightData.RollAngleDeg = Math.Clamp(_previousRoll + _rollDeltaDeg, -45.0, 45.0);
    }

    public void Undo(AircraftModel aircraft)
    {
        aircraft.FlightData.RollAngleDeg = _previousRoll;
    }
}