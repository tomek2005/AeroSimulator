using System;
using System.Collections.Generic;

namespace AeroSimulator.Core.Events;

public class EventBus
{
    // Lazy initialization (wymóg na ocenę wyższą niż dostateczną dla Singletona)
    private static readonly Lazy<EventBus> _instance = new(() => new EventBus());
    public static EventBus Instance => _instance.Value;

    private readonly List<IFlightEventHandler> _handlers = new();

    // Prywatny konstruktor uniemożliwia tworzenie instancji operatorem 'new'
    private EventBus() { }

    public void Subscribe(IFlightEventHandler handler)
    {
        if (!_handlers.Contains(handler))
        {
            _handlers.Add(handler);
        }
    }

    public void Unsubscribe(IFlightEventHandler handler)
    {
        if (_handlers.Contains(handler))
        {
            _handlers.Remove(handler);
        }
    }

    public void Publish(FlightEvent evt)
    {
        // Kopia listy zabezpiecza przed błędami, jeśli handler spróbuje się wyrejestrować w trakcie pętli
        var activeHandlers = new List<IFlightEventHandler>(_handlers);
        foreach (var handler in activeHandlers)
        {
            handler.Handle(evt);
        }
    }
}