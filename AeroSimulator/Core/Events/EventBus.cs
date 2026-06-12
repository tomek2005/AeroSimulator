using System;
using System.Collections.Generic;

namespace AeroSimulator.Core.Events;

/// <summary>
/// WZORZEC: OBSERVER + SINGLETON
/// Centralna szyna zdarzeń. Pozwala komponentom komunikować się bez bezpośrednich zależności.
/// </summary>
public class EventBus
{
    // Leniwa inicjalizacja (Zalicza punkt: "EventBus with lazy init")
    private static readonly Lazy<EventBus> _instance = new(() => new EventBus());
    public static EventBus Instance => _instance.Value;

    private readonly List<IFlightEventHandler> _handlers = new();
    private readonly object _lock = new(); // "Kłódka" dla bezpieczeństwa wielowątkowego

    // Prywatny konstruktor uniemożliwia tworzenie instancji operatorem 'new'
    private EventBus() { }

    public void Subscribe(IFlightEventHandler handler)
    {
        if (handler == null) return;

        // Bezpieczne dodawanie z kłódką
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

        // Bezpieczne usuwanie z kłódką
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

        // Kopiujemy listę błyskawicznie wewnątrz "kłódki"
        lock (_lock)
        {
            activeHandlers = new List<IFlightEventHandler>(_handlers);
        }

        // Informujemy wszystkie nasłuchujące systemy (już poza kłódką, aby nie spowalniać gry)
        foreach (var handler in activeHandlers)
        {
            handler.Handle(evt);
        }
    }
}
