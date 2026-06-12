using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class EngineExplosionEvent : FlightEvent
{
    public int EngineNumber { get; init; }

    public EngineExplosionEvent(int engineNumber, string message)
    {
        EngineNumber = engineNumber;
        Source = "Engines";
        Level = Severity.Critical;
        Message = message;
    }
}