using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class EngineFireEvent : FlightEvent
{
    public int EngineNumber { get; init; }

    public EngineFireEvent(int engineNumber, string message) 
    {
        EngineNumber = engineNumber;
        Source = "Engines";
        Level = Severity.Critical;
        Message = message;
    }
}