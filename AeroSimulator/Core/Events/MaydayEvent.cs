namespace AeroSimulator.Core.Events;

using System;
using AeroSimulator.Core.Aircraft.Enums;

/// <summary>
/// Zdarzenie wywoływane w momencie deklaracji stanu awaryjnego (MAYDAY).
/// </summary>
public class MaydayEvent : FlightEvent
{
    // Dziedziczy Timestamp, Source, Level i Message po klasie bazowej FlightEvent.
    // Dodajemy tylko pole z powodem:
    public string Reason { get; init; } = string.Empty;
}