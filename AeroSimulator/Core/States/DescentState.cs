namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class DescentState : IAircraftState
{
    public string StateName => "DESCENT";
    public string StateDescription => "Schodzenie z wysokości przelotowej i przygotowanie do lądowania.";
    public ConsoleColor StateColor => ConsoleColor.DarkMagenta;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land", "Abort", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Ustawienie przepustnicy na 30%
        ctx.FlightData.Throttle = 0.3; 
        
        // Rozpoczęcie wysuwania klap (inkrementalnie)
        // ctx.Hydraulics.SetFlaps(FlapPosition.Position1); // (Zależnie od pełnej implementacji Hydraulics)
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Zmniejszanie wysokości i prędkości (symulacja fizyki zniżania)
        double descentRate = ctx.Config.NormalDescentFtMin / 60.0; // stopy na sekundę
        ctx.FlightData.Altitude -= descentRate * deltaT;
        ctx.FlightData.Speed -= 5.0 * deltaT; // Stopniowa utrata prędkości

        // Warunek automatycznego przejścia do lądowania
        if (ctx.FlightData.Altitude < 3000.0 && ctx.FlightData.Speed < 180.0)
        {
            Land(ctx);
        }
    }

    public void Land(Aircraft ctx)
    {
        ctx.TransitionTo(new LandingState());
    }

    public void Abort(Aircraft ctx)
    {
        // Go-around (przerwanie podejścia)
        ctx.TransitionTo(new ClimbState());
    }

    public void HandleEmergency(Aircraft ctx) => ctx.TransitionTo(new EmergencyState());

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { } // Już schodzimy

    public void OnExit(Aircraft ctx) { }
}