using System;
using System.Collections.Generic;

namespace AeroSimulator.Core.Events;

// WZORZEC: OBSERVER
// Centralna szyna zdarzeń. Pozwala komponentom komunikować się bez bezpośrednich zależności.
public class EventBus
{
    private static readonly Lazy<EventBus> _instance = new(() => new EventBus());
    public static EventBus Instance => _instance.Value;

    private readonly List<IFlightEventHandler> _handlers = new();
    private readonly object _lock = new();

    private EventBus()
    {
    }

    public void Subscribe(IFlightEventHandler handler)
    {
        if (handler == null) return;
        
        lock (_lock)
        {
            if (!_handlers.Contains(handler))
            {
                _handlers.Add(handler);
            }
        }
    }

    public void Unsubscribe(IFlightEventHandler handler)
    {
        if (handler == null) return;
        
        lock (_lock)
        {
            _handlers.Remove(handler);
        }
    }

    public void ClearHandlers()
    {
        lock (_lock)
        {
            _handlers.Clear();
        }
    }

    public void Publish(FlightEvent evt)
    {
        if (evt == null) return;

        List<IFlightEventHandler> activeHandlers;
        
        lock (_lock)
        {
            activeHandlers = new List<IFlightEventHandler>(_handlers);
        }
        
        foreach (var handler in activeHandlers)
        {
            handler.Handle(evt);
        }
    }
}