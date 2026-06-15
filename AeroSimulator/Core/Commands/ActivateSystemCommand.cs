using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class ActivateSystemCommand : IFlightCommand
{
    private readonly Action<AircraftModel> _action;
    private readonly string _name;
    private readonly string _details;

    public ActivateSystemCommand(string name, string details, Action<AircraftModel> action)
    {
        _name = name;
        _details = details;
        _action = action;
    }

    public string Name => _name;
    public string Details => _details;

    public void Execute(AircraftModel aircraft) => _action(aircraft);
    public void Undo(AircraftModel aircraft) { }
}
