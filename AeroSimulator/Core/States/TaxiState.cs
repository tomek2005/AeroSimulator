namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class TaxiState : IAircraftState
{
    public string StateName => "TAXI";
    public string StateDescription => "Kołowanie na pas startowy.";
    public ConsoleColor StateColor => ConsoleColor.DarkYellow;
    public IReadOnlyList<string> AllowedActions => new List<string> { "TakeOff", "Abort" };

    public void OnEnter(Aircraft ctx)
    {
        ctx.FlightData.Speed = 15; // Wymuszamy 15 węzłów
    }

    public void TakeOff(Aircraft ctx)
    {
        // Gracz wcisnął NextPhase (Start), gdy dotarł na pas
        ctx.TransitionTo(new TakeOffState());
    }

    public void Abort(Aircraft ctx)
    {
        // Anulowanie kołowania i powrót do bramki
        ctx.TransitionTo(new GroundState());
    }

    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }
    public void HandleEmergency(Aircraft ctx) { }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Przesuwanie pozycji samolotu na mapie lotniska 
        // np. ctx.FlightData.MapX += (15.0 * deltaT)
    }

    public void OnExit(Aircraft ctx)
    {
        // Zatrzymujemy samolot przed startem
        ctx.FlightData.Speed = 0; 
    }
}