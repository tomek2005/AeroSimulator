using System;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events.Handlers;

public class StatisticsHandler : IFlightEventHandler
{
    public static int TotalEventsCount { get; private set; }
    public static int CriticalFailuresCount { get; private set; }
    public static int StateTransitionsCount { get; private set; }
    public static int AnomaliesTriggeredCount { get; private set; }

    public void Handle(FlightEvent evt)
    {
        TotalEventsCount++;

        if (evt.Level == Severity.Critical)
        {
            CriticalFailuresCount++;
        }

        if (evt is StateChangedEvent)
        {
            StateTransitionsCount++;
        }

        if (evt is AnomalyTriggeredEvent)
        {
            AnomaliesTriggeredCount++;
        }
    }

    public static void Reset()
    {
        TotalEventsCount = 0;
        CriticalFailuresCount = 0;
        StateTransitionsCount = 0;
        AnomaliesTriggeredCount = 0;
    }
}