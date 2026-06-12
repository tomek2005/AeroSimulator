using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class CascadeTriggeredEvent : FlightEvent
{
    public string SourceAnomaly { get; init; } = string.Empty;
    public string TargetAnomaly { get; init; } = string.Empty;

    public CascadeTriggeredEvent(string sourceAnomaly, string targetAnomaly, string message)
    {
        SourceAnomaly = sourceAnomaly;
        TargetAnomaly = targetAnomaly;
        Source = "CascadeHandler";
        Level = Severity.Critical;
        Message = message;
    }
}