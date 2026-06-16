using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record SystemFailureEvent : FlightEvent
{
    public string SystemName { get; init; } = string.Empty;
    public double HealthRemaining { get; init; }

    // Przekazanie wspólnych danych do niemutowalnego konstruktora z FlightEvent
    public SystemFailureEvent(string systemName, double health, string message)
        : base(message, "Systems", Severity.Critical)
    {
        SystemName = systemName;
        HealthRemaining = health;
    }
}