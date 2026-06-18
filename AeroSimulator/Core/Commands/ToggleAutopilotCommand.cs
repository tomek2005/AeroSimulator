using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class ToggleAutopilotCommand : IFlightCommand
{
    private bool _wasEngaged;

    public string Name => "ToggleAutopilot";
    public string Details => "Autopilot toggled";

    public void Execute(AircraftModel aircraft)
    {
        _wasEngaged = aircraft.AutopilotSystem.IsEngaged;

        if (_wasEngaged)
        {
            aircraft.AutopilotSystem.Disengage();
        }
        else
        {
            aircraft.AutopilotSystem.Engage(
                aircraft.FlightData.Altitude,
                aircraft.FlightData.Heading,
                aircraft.FlightData.Speed);
        }
    }

    public void Undo(AircraftModel aircraft)
    {
        if (_wasEngaged)
        {
            aircraft.AutopilotSystem.Engage(
                aircraft.FlightData.Altitude,
                aircraft.FlightData.Heading,
                aircraft.FlightData.Speed);
        }
        else
        {
            aircraft.AutopilotSystem.Disengage();
        }
    }
}