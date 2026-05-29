namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class HoldingState : IAircraftState
{
    public string StateName => "HOLDING";
    public string StateDescription => "Krążenie w strefie oczekiwania.";
    public ConsoleColor StateColor => ConsoleColor.DarkCyan;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Zapisanie aktualnego punktu holdingu (racetrack pattern)
        // Kąt przechylenia 25 stopni
        ctx.FlightData.RollAngleDeg = 25.0;
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Symulacja pętli (racetrack pattern) i spalanie paliwa
        
        // Krytyczny poziom paliwa wymusza awaryjną reakcję
        if (ctx.FlightData.FuelRemainingPercent() < 5.0)
        {
            HandleEmergency(ctx);
        }
    }

    public void Land(Aircraft ctx) => ctx.TransitionTo(new DescentState());

    public void HandleEmergency(Aircraft ctx)
    {
        // Wg README awaria w holdingu (Fuel critical) oznacza NATYCHMIASTOWE przejście do DescentState
        ctx.TransitionTo(new DescentState());
    }

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Abort(Aircraft ctx) { }
    
    public void OnExit(Aircraft ctx)
    {
        // Wyrównanie skrzydeł przy wyjściu z holdingu
        ctx.FlightData.RollAngleDeg = 0.0;
    }
}