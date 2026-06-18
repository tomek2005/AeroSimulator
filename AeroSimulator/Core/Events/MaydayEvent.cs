using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record MaydayEvent : FlightEvent
{
    public EmergencyType EmergencyType { get; init; }
    public string DeclaredBy { get; init; } = string.Empty;
    
    public MaydayEvent(EmergencyType type, string declaredBy, string message)
        : base(message, "Crew", Severity.Critical)
    {
        EmergencyType = type;
        DeclaredBy = declaredBy;
    }
}