using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record CommandExecutedEvent : FlightEvent
{
    public string CommandName { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    
    public CommandExecutedEvent(string commandName, string details, string message)
        : base(message, "Commands", Severity.Info)
    {
        CommandName = commandName;
        Details = details;
    }
}