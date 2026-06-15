using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class SetPitchCommand : IFlightCommand
{
    private readonly double _deltaDeg;
    private double _previousPitch;

    public SetPitchCommand(double deltaDeg)
    {
        _deltaDeg = deltaDeg;
    }

    public string Name => "SetPitch";
    public string Details => _deltaDeg >= 0 ? "Pitch up" : "Pitch down";

    public void Execute(AircraftModel aircraft)
    {
        _previousPitch = aircraft.FlightData.PitchAngleDeg;
        aircraft.FlightData.PitchAngleDeg = Math.Clamp(_previousPitch + _deltaDeg, -45.0, 45.0);
    }

    public void Undo(AircraftModel aircraft)
    {
        aircraft.FlightData.PitchAngleDeg = _previousPitch;
    }
}
