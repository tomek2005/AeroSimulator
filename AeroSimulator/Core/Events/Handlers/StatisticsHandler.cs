using System;
using System.Collections.Generic;
using System.Linq;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events.Handlers;

public record FlightStatistics(
    int TotalAnomalies,
    int TotalCascades,
    int TotalFailures,
    int StateTransitions,
    int CriticalAlerts
);

public static class StatisticsHandler
{
    public static FlightStatistics GenerateReport(IEnumerable<FlightEvent> eventLog)
    {
        return eventLog.Aggregate(
            seed: new FlightStatistics(0, 0, 0, 0, 0),
            func: (acc, evt) => new FlightStatistics(
                TotalAnomalies: acc.TotalAnomalies + (evt is AnomalyTriggeredEvent ? 1 : 0),
                TotalCascades: acc.TotalCascades + (evt is CascadeTriggeredEvent ? 1 : 0),
                TotalFailures: acc.TotalFailures + (evt is SystemFailureEvent ? 1 : 0),
                StateTransitions: acc.StateTransitions + (evt is StateChangedEvent ? 1 : 0),
                CriticalAlerts: acc.CriticalAlerts + (evt.Level == Severity.Critical ? 1 : 0)
            )
        );
    }
}