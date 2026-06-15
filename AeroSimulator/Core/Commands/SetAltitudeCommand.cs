using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class SetAltitudeCommand : IFlightCommand
{
    private readonly double _targetAltitude;
    private double _previousTarget;

    public SetAltitudeCommand(double targetAltitude)
    {
        _targetAltitude = targetAltitude;
    }

    public string Name => "SetAltitude";
    public string Details => $"Target altitude {_targetAltitude:0} ft";

    public void Execute(AircraftModel aircraft)
    {
        _previousTarget = aircraft.FlightData.TargetAltitude;
        aircraft.FlightData.TargetAltitude = _targetAltitude;
        aircraft.AutopilotSystem.SetTargetAltitude(_targetAltitude);
    }

    public void Undo(AircraftModel aircraft)
    {
        aircraft.FlightData.TargetAltitude = _previousTarget;
        aircraft.AutopilotSystem.SetTargetAltitude(_previousTarget);
    }
}
