using System;
using System.Collections.Generic;
using System.Linq;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events.Handlers;

// 1. Zastosowanie FP: Zwracamy Niezmienny Rekord (Immutable Record)
public record FlightStatistics(
    int TotalAnomalies,
    int TotalCascades,
    int TotalFailures,
    int StateTransitions,
    int CriticalAlerts
);

public static class StatisticsHandler
{
    // 2. Zastosowanie FP: Czysta Funkcja i Potoki Deklaratywne (LINQ Pipelines)
    // Funkcja nie mutuje żadnego zewnętrznego stanu, tylko przyjmuje logi i zwraca nowy wynik.
    public static FlightStatistics GenerateReport(IEnumerable<FlightEvent> eventLog)
    {
        return new FlightStatistics(
            // Wykorzystanie funkcji wyższego rzędu: OfType (filtrowanie po typie) i Count (agregacja)
            TotalAnomalies: eventLog.OfType<AnomalyTriggeredEvent>().Count(),
            TotalCascades: eventLog.OfType<CascadeTriggeredEvent>().Count(),
            TotalFailures: eventLog.OfType<SystemFailureEvent>().Count(),
            StateTransitions: eventLog.OfType<StateChangedEvent>().Count(),
            
            // Dodatkowy potok: zliczanie wszystkich komunikatów z poziomem "Critical"
            CriticalAlerts: eventLog.Count(e => e.Level == Severity.Critical)
        );
    }
}