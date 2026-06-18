using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record WingFireEvent : FlightEvent
{
    public string Side { get; init; } = string.Empty;
    
    public WingFireEvent(string side, string message)
        : base(message, "Wings", Severity.Critical)
    {
        Side = side;
    }
}