using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record EngineExplosionEvent : FlightEvent
{
    public int EngineNumber { get; init; }
    
    public EngineExplosionEvent(int engineNumber, string message)
        : base(message, "Engines", Severity.Critical)
    {
        EngineNumber = engineNumber;
    }
}