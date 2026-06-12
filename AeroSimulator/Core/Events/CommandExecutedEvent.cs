using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class CommandExecutedEvent : FlightEvent
{
    public string CommandName { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;

    public CommandExecutedEvent(string commandName, string details, string message)
    {
        CommandName = commandName;
        Details = details;
        Source = "Commands";
        Level = Severity.Info;
        Message = message;
    }
}