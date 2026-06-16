using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record TelemetryTickEvent : FlightEvent
{
    // Przekazanie danych bezpośrednio do bazowego, niemutowalnego konstruktora
    public TelemetryTickEvent(string message)
        : base(message, "Telemetry", Severity.Low)
    {
    }
}