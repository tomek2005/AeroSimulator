using AeroSimulator.Core.Events;
using AircraftModel = AeroSimulator.Core.Aircraft.Aircraft;

namespace AeroSimulator.Core.Commands;

public class CommandHistory
{
    private readonly Stack<IFlightCommand> _undoStack = new();
    private readonly List<IFlightCommand> _executed = new();

    public IReadOnlyList<IFlightCommand> Executed => _executed.AsReadOnly();

    public void Execute(IFlightCommand command, AircraftModel aircraft)
    {
        command.Execute(aircraft);
        _executed.Add(command);
        _undoStack.Push(command);

        aircraft.Publish(new CommandExecutedEvent(
            command.Name,
            command.Details,
            $"{command.Name}: {command.Details}"));
    }

    public bool UndoLast(AircraftModel aircraft)
    {
        if (_undoStack.Count == 0) return false;

        var command = _undoStack.Pop();
        command.Undo(aircraft);

        aircraft.Publish(new CommandExecutedEvent(
            "Undo",
            command.Name,
            $"Undo: {command.Name}"));

        return true;
    }
}