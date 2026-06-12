using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Events;

public class CriticalState : IAircraftState
{
    public string StateName => "CRITICAL";
    public string StateDescription => "CRITICAL SYSTEMS FAILURE. Immediate action required to survive.";
    public ConsoleColor StateColor => ConsoleColor.Red;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land" };

    public void OnEnter(Aircraft ctx)
    {
        // FAKTYCZNE odcięcie autopilota
        if (!ctx.AutopilotSystem.IsOffline)
        {
            ctx.AutopilotSystem.SetOffline();
        }
        
        ctx.PublishAlert("CRITICAL: FLIGHT ENVELOPE COMPROMISED. MANUAL CONTROL ONLY.", Severity.Critical);
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // FAKTYCZNE ograniczenie sterowności i drastyczny spadek parametrów
        // Samolot wymyka się spod kontroli, zaczyna coraz szybciej opadać i tracić prędkość
        ctx.FlightData.VerticalSpeed -= 500.0 * deltaT; 
        ctx.FlightData.Speed -= 15.0 * deltaT;
        
        // Znoszenie w trybie Critical jest ekstremalne (mnożnik 4.0)
        if (ctx.DamageModel.AsymmetricDragActive)
        {
            ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec * 4.0, deltaT);
        }

        // W stanie krytycznym co krok sprawdzamy warunek całkowitej katastrofy strukturalnej
        if (ctx.DamageModel.IsGameOver)
        {
            // Pętla główna wychwyci flagę GameOver i odpali sekwencję czarnej skrzynki.
            // Wysyłamy ostatni komunikat przed zniszczeniem:
            ctx.PublishAlert("FATAL IMPACT IMMINENT. SYSTEM TERMINATION.", Severity.Critical);
        }
    }

    public void Land(Aircraft ctx)
    {
        // Próba awaryjnego posadzenia maszyny "w polu" / crash landing
        ctx.TransitionTo(new LandingState());
    }

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void HandleEmergency(Aircraft ctx) { }
    public void Abort(Aircraft ctx) { }
    public void OnExit(Aircraft ctx) { }
}