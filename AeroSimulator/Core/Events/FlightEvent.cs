using System;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public abstract record FlightEvent
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Source { get; init; } = string.Empty;
    public Severity Level { get; init; } = Severity.Low;
    public string Message { get; init; } = string.Empty;

    protected FlightEvent() { }

    protected FlightEvent(string message, string source, Severity level)
    {
        Message = message;
        Source = source;
        Level = level;
    }
}