using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record MaydayEvent : FlightEvent
{
    public EmergencyType EmergencyType { get; init; }
    public string DeclaredBy { get; init; } = string.Empty;

    // Przekazanie wspólnych danych (Source="Crew", Level=Critical) do bazowego, niemutowalnego konstruktora
    public MaydayEvent(EmergencyType type, string declaredBy, string message)
        : base(message, "Crew", Severity.Critical)
    {
        EmergencyType = type;
        DeclaredBy = declaredBy;
    }
}