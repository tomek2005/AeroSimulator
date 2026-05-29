namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Aircraft.Enums;

public class EmergencyState : IAircraftState
{
    // Pola prywatne zgodnie ze specyfikacją
    private double _severity;
    private bool _maydayDeclared;

    public string StateName => "EMERGENCY";
    public string StateDescription => "Stan awaryjny (MAYDAY). Konieczność podjęcia szybkich działań.";
    public ConsoleColor StateColor => ConsoleColor.Red;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Publikacja zdarzenia MaydayEvent na EventBus
        EventBus.Instance.Publish(new MaydayEvent
        {
            Timestamp = DateTime.Now,
            Source = "EmergencyState",
            Level = Severity.Critical,
            Message = "MAYDAY MAYDAY MAYDAY",
            Reason = "Zadeklarowano stan awaryjny",
            // Type = EmergencyType.General // (Zależnie od dostępnego enuma)
        });

        _maydayDeclared = true;
        _severity = 0.5; // Początkowy poziom powagi awarii
        
        Console.WriteLine("ALERT: MAYDAY DECLARED. Poinformowano ATC.");
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Symulacja pogarszającej się sytuacji w przypadku braku reakcji gracza
        // Śledzenie aktywnej anomalii (w pełnej implementacji pobierane z AnomalyEngine)
        _severity += 0.05 * deltaT; // Powaga rośnie z czasem

        // Jeżeli awaria eskaluje i wskaźnik osiągnie wysoki próg -> przejście do CriticalState
        if (_severity >= 1.0)
        {
            HandleEmergency(ctx);
        }
    }

    public void Land(Aircraft ctx)
    {
        // Natychmiastowe wymuszenie lądowania (z pominięciem standardowego DescentState)
        ctx.TransitionTo(new LandingState());
    }

    public void HandleEmergency(Aircraft ctx)
    {
        // Dalsza eskalacja - awaria staje się krytyczna
        ctx.TransitionTo(new CriticalState());
    }

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Abort(Aircraft ctx) { }
    
    public void OnExit(Aircraft ctx) { }
}