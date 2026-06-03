using System;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public abstract class FlightEvent
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Source { get; set; } = string.Empty;
    public Severity Level { get; set; } = Severity.Low;
    public string Message { get; set; } = string.Empty;

    protected FlightEvent()
    {
    }

    protected FlightEvent(string message)
    {
        Message = message;
    }
}

public class StateChangedEvent : FlightEvent
{
    public string OldState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
}

public class SystemFailureEvent : FlightEvent
{
    public string SystemName { get; }
    public double Severity { get; }

    public SystemFailureEvent(string systemName, double severityValue, string message) 
        : base(message) 
    {
        SystemName = systemName;
        Severity = severityValue;
        Source = "Systems";
        
        Level = AeroSimulator.Core.Aircraft.Enums.Severity.High; 
    }
}

public class AnomalyTriggeredEvent : FlightEvent
{
    public string AnomalyName { get; }
    public string SeverityLevel { get; }

    public AnomalyTriggeredEvent(string anomalyName, string severityLevel, string message) 
        : base(message)
    {
        AnomalyName = anomalyName;
        SeverityLevel = severityLevel;
        Source = "Anomalies";
        Level = Severity.Medium;
    }
}

public class EngineFireEvent : FlightEvent
{
    public int EngineNumber { get; }

    public EngineFireEvent(int engineNumber, string message) 
        : base(message) 
    {
        EngineNumber = engineNumber;
        Source = "Engines";
        Level = Severity.Critical;
    }
}