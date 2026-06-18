using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record SystemFailureEvent : FlightEvent
{
    public string SystemName { get; init; } = string.Empty;
    public double HealthRemaining { get; init; }
    
    public SystemFailureEvent(string systemName, double health, string message)
        : base(message, "Systems", Severity.Critical)
    {
        SystemName = systemName;
        HealthRemaining = health;
    }
}