using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record EngineFireEvent : FlightEvent
{
    public int EngineNumber { get; init; }

    // Przekazanie wspólnych danych do niemutowalnego konstruktora z FlightEvent
    public EngineFireEvent(int engineNumber, string message) 
        : base(message, "Engines", Severity.Critical)
    {
        EngineNumber = engineNumber;
    }
}