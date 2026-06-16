using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record CascadeTriggeredEvent : FlightEvent
{
    // Właściwości są już gotowe (używają 'init')
    public string SourceAnomaly { get; init; } = string.Empty;
    public string TargetAnomaly { get; init; } = string.Empty;

    // Przekazanie wspólnych danych do niemutowalnego konstruktora z FlightEvent
    public CascadeTriggeredEvent(string sourceAnomaly, string targetAnomaly, string message)
        : base(message, "CascadeHandler", Severity.Critical)
    {
        SourceAnomaly = sourceAnomaly;
        TargetAnomaly = targetAnomaly;
    }
}