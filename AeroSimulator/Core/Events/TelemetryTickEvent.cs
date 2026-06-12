using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class TelemetryTickEvent : FlightEvent
{
    public TelemetryTickEvent(string message)
    {
        Source = "Telemetry";
        Level = Severity.Low;
        Message = message;
    }
}