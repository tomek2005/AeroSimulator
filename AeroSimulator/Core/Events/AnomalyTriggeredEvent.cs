using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class AnomalyTriggeredEvent : FlightEvent
{
    public string AnomalyName { get; init; } = string.Empty;

    public AnomalyTriggeredEvent(string anomalyName, Severity level, string message) 
    {
        AnomalyName = anomalyName;
        Source = "Anomalies";
        Level = level;
        Message = message;
    }
}