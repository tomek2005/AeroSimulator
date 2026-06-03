using System;
using AeroSimulator.Core.Aircraft;

namespace AeroSimulator.Core.Events.Handlers;

public class CascadeHandler : IFlightEventHandler
{
    private readonly Aircraft.Aircraft _aircraft; // Referencja do modelu samolotu
    private readonly Random _rng = new();

    public CascadeHandler(Aircraft.Aircraft aircraft)
    {
        _aircraft = aircraft;
    }

    public void Handle(FlightEvent evt)
    {
        // Mapowanie zdarzeń na reguły kaskadowe ze specyfikacji (Sekcja 3.1)
        if (evt is AnomalyTriggeredEvent anomalyEvt)
        {
            EvaluateAnomalyCascade(anomalyEvt);
        }
        else if (evt is SystemFailureEvent failureEvt)
        {
            EvaluateSystemFailureCascade(failureEvt);
        }
    }

    private void EvaluateAnomalyCascade(AnomalyTriggeredEvent evt)
    {
        // Reguła: BirdStrikeAnomaly -> 40% szans na pożar silnika
        if (evt.AnomalyName == "BirdStrikeAnomaly")
        {
            if (_rng.NextDouble() < 0.40)
            {
                _aircraft.Publish(new EngineFireEvent(1, "CRITICAL: Bird strike ignited Engine 1!"));
            }
        }
    }

    private void EvaluateSystemFailureCascade(SystemFailureEvent evt)
    {
        // Reguła: Awaria elektryki wyłącza autopilot
        if (evt.SystemName == "ElectricalSystem" && evt.Severity > 0.5)
        {
            // Tutaj możesz bezpośrednio wywołać metodę wyłączającą AP w obiekcie aircraft
            // np. _aircraft.Autopilot.Disable();
        }
    }
}