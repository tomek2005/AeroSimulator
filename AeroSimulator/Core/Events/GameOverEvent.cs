namespace AeroSimulator.Core.Events;

using System;
using AeroSimulator.Core.Aircraft.Enums;

/// <summary>
/// Specjalne zdarzenie katastrofalne, wywoływane w momencie bezpowrotnego zniszczenia maszyny.
/// </summary>
public class GameOverEvent : FlightEvent
{
    // Dziedziczy Timestamp, Source, Level i Message z klasy bazowej FlightEvent.
    // Dodajemy tylko pole dedykowane dla powodu końca gry:
    public string Reason { get; init; } = string.Empty;

    public GameOverEvent(string reason)
    {
        Reason = reason;
        Source = "DamageModel";
        Level = Severity.Critical;
        Message = $"GAME OVER: {reason}";
    }
}
