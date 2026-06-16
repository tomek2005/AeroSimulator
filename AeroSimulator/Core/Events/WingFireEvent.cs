using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record WingFireEvent : FlightEvent
{
    public string Side { get; init; } = string.Empty;

    // Przekazanie wspólnych danych do niemutowalnego konstruktora z FlightEvent
    public WingFireEvent(string side, string message)
        : base(message, "Wings", Severity.Critical)
    {
        Side = side;
    }
}