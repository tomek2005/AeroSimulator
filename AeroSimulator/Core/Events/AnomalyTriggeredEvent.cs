using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record AnomalyTriggeredEvent : FlightEvent
{
    public string AnomalyName { get; init; } = string.Empty;
    
    public AnomalyTriggeredEvent(string anomalyName, Severity level, string message)
        : base(message, "Anomalies", level)
    {
        AnomalyName = anomalyName;
    }
}