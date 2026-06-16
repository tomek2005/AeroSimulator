using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record StateChangedEvent : FlightEvent
{
    public string OldState { get; init; } = string.Empty;
    public string NewState { get; init; } = string.Empty;

    // Przekazanie wspólnych danych do niemutowalnego konstruktora z FlightEvent
    public StateChangedEvent(string oldState, string newState, string message)
        : base(message, "Aircraft", Severity.Info)
    {
        OldState = oldState;
        NewState = newState;
    }
}