using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record StateChangedEvent : FlightEvent
{
    public string OldState { get; init; } = string.Empty;
    public string NewState { get; init; } = string.Empty;
    
    public StateChangedEvent(string oldState, string newState, string message)
        : base(message, "Aircraft", Severity.Info)
    {
        OldState = oldState;
        NewState = newState;
    }
}