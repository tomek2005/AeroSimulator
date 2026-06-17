using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record EngineFireEvent : FlightEvent
{
    public int EngineNumber { get; init; }
    
    public EngineFireEvent(int engineNumber, string message)
        : base(message, "Engines", Severity.Critical)
    {
        EngineNumber = engineNumber;
    }
}