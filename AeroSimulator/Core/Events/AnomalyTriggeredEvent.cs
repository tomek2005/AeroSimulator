using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record AnomalyTriggeredEvent : FlightEvent
{
    // Właściwość ma już przypisane 'init', więc jest gotowa na FP
    public string AnomalyName { get; init; } = string.Empty;

    // Przekazanie wspólnych pól do konstruktora bazowego FlightEvent
    public AnomalyTriggeredEvent(string anomalyName, Severity level, string message) 
        : base(message, "Anomalies", level)
    {
        AnomalyName = anomalyName;
    }
}