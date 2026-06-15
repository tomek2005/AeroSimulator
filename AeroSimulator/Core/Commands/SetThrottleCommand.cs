using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class SetThrottleCommand : IFlightCommand
{
    private readonly double _delta;
    private double _previousThrottle;

    public SetThrottleCommand(double delta)
    {
        _delta = delta;
    }

    public string Name => "SetThrottle";
    public string Details => _delta >= 0 ? "Throttle increased" : "Throttle decreased";

    public void Execute(AircraftModel aircraft)
    {
        _previousThrottle = aircraft.FlightData.Throttle;
        aircraft.FlightData.Throttle = Math.Clamp(_previousThrottle + _delta, 0.0, 1.0);
    }

    public void Undo(AircraftModel aircraft)
    {
        aircraft.FlightData.Throttle = _previousThrottle;
    }
}
