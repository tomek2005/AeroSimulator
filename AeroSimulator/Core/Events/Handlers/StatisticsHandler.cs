using System;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events.Handlers;

public class StatisticsHandler : IFlightEventHandler
{
    public static int TotalAnomalies { get; private set; }
    public static int TotalCascades { get; private set; }
    public static int TotalFailures { get; private set; }
    public static int StateTransitions { get; private set; }
    public static DateTime SimulationStartTime { get; private set; } = DateTime.Now;

    public void Handle(FlightEvent evt)
    {
        switch (evt)
        {
            case AnomalyTriggeredEvent: TotalAnomalies++; break;
            case CascadeTriggeredEvent: TotalCascades++; break;
            case SystemFailureEvent: TotalFailures++; break;
            case StateChangedEvent: StateTransitions++; break;
        }
    }

    public static void Reset()
    {
        TotalAnomalies = 0; TotalCascades = 0; TotalFailures = 0; StateTransitions = 0;
        SimulationStartTime = DateTime.Now;
    }
}