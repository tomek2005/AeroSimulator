using System;
using System.Collections.Generic;

namespace AeroSimulator.Core.Events.Handlers;

public class BlackBoxHandler : IFlightEventHandler
{
    private static readonly List<FlightEvent> _recordedEvents = new();

    public static IReadOnlyList<FlightEvent> RecordedEvents => _recordedEvents;

    public void Handle(FlightEvent evt)
    {
        _recordedEvents.Add(evt);
    }

    public static void Clear()
    {
        _recordedEvents.Clear();
    }
}