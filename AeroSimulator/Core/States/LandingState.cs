namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class LandingState : IAircraftState
{
    // Pola prywatne zalecone w specyfikacji
    private double _ilsDeviation;
    private double _touchdownSpeed;
    // private LandingPhase _phase; // Opcjonalnie do podziału na Approach/Flare/Rollout

    public string StateName => "LANDING";
    public string StateDescription => "Podejście finałowe (ILS) i przyziemienie.";
    public ConsoleColor StateColor => ConsoleColor.Magenta;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Abort", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Pełne klapy i wypuszczenie podwozia
        // ctx.Hydraulics.DeployGear();
        // ctx.Hydraulics.SetFlaps(FlapPosition.Full);
        
        // Zapisanie optymalnej prędkości przyziemienia
        _touchdownSpeed = ctx.Config.VRSpeedKts * 0.9; // Uproszczona logika
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // 1. Śledzenie ścieżki schodzenia (Glideslope)
        ctx.FlightData.Altitude -= (700.0 / 60.0) * deltaT; // Schodzenie ok 700 ft/min
        
        // 2. Obsługa wiatru bocznego (Crosswind) - symulacja wibracji/znoszenia
        // ctx.FlightData.Heading += crosswindDrift * deltaT;

        // 3. Asymetryczny opór z palącego się skrzydła (najtrudniejsza część)
        if (ctx.DamageModel.AsymmetricDragActive)
        {
            // Pilot ma dużo trudniej z wyrównaniem samolotu przed pasem
            ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec * 1.5, deltaT); 
        }

        // 4. Flare (Wyrównanie przed przyziemieniem - poniżej 50 stóp)
        if (ctx.FlightData.Altitude <= 50.0 && ctx.FlightData.Altitude > 0)
        {
            ctx.FlightData.PitchAngleDeg = 3.0; // Delikatne zadarcie nosa
            ctx.FlightData.Throttle = 0.0;      // Przepustnica na zero (Idle)
        }

        // 5. Touchdown (Przyziemienie)
        if (ctx.FlightData.Altitude <= 0)
        {
            ctx.FlightData.Altitude = 0;
            
            // Rejestracja zdarzenia LandingCompletedEvent byłaby tutaj wysyłana na EventBus
            
            // Zakończenie lotu i przejście do stanu na ziemi
            ctx.TransitionTo(new GroundState());
        }
    }

    public void Abort(Aircraft ctx)
    {
        // Go-around (Odejście na drugi krąg)
        ctx.FlightData.Throttle = 1.0; // Pełny ciąg
        // ctx.Hydraulics.RetractGear(); // Schowanie podwozia
        // ctx.Hydraulics.SetFlaps(FlapPosition.Position1);
        
        ctx.TransitionTo(new ClimbState());
    }

    public void HandleEmergency(Aircraft ctx) => ctx.TransitionTo(new EmergencyState());

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) { } // Już lądujemy

    public void OnExit(Aircraft ctx)
    {
        // Reset ewentualnych parametrów ILS
        _ilsDeviation = 0;
    }
}