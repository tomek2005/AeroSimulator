using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record GameOverEvent : FlightEvent
{
    public string Reason { get; init; } = string.Empty;

    // Zmieniamy konstruktor, aby przyjmował TYLKO 'reason' (jeden argument)
    // i przekazywał go wyżej jako treść wiadomości.
    public GameOverEvent(string reason)
        : base($"GAME OVER: {reason}", "DamageModel", Severity.Critical)
    {
        Reason = reason;
    }
}