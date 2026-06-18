using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record CascadeTriggeredEvent : FlightEvent
{
    public string SourceAnomaly { get; init; } = string.Empty;
    public string TargetAnomaly { get; init; } = string.Empty;
    
    public CascadeTriggeredEvent(string sourceAnomaly, string targetAnomaly, string message)
        : base(message, "CascadeHandler", Severity.Critical)
    {
        SourceAnomaly = sourceAnomaly;
        TargetAnomaly = targetAnomaly;
    }
}