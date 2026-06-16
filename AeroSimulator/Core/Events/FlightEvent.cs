using System;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public abstract record FlightEvent
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Source { get; set; } = string.Empty;
    public Severity Level { get; set; } = Severity.Low;
    public string Message { get; set; } = string.Empty;

    protected FlightEvent() { }

    protected FlightEvent(string message, string source, Severity level)
    {
        Message = message;
        Source = source;
        Level = level;
    }
}