using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class WingFireEvent : FlightEvent
{
    public string Side { get; init; } = string.Empty;

    public WingFireEvent(string side, string message)
    {
        Side = side;
        Source = "Wings";
        Level = Severity.Critical;
        Message = message;
    }
}