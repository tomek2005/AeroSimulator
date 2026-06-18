using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record TelemetryTickEvent : FlightEvent
{
    public TelemetryTickEvent(string message)
        : base(message, "Telemetry", Severity.Low)
    {
    }
}