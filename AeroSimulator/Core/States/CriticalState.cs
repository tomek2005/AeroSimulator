namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Events;

public class CriticalState : IAircraftState
{
    public string StateName => "CRITICAL";
    public string StateDescription => "Sytuacja krytyczna! Groźba całkowitego zniszczenia struktury!";
    public ConsoleColor StateColor => ConsoleColor.DarkRed;
    
    // Zgodnie ze specyfikacją: czyszczenie większości dozwolonych akcji
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land" };

    public void OnEnter(Aircraft ctx)
    {
        // Wyłączenie autopilota
        // ctx.GetSystem(SystemType.Autopilot)?.ApplyDamage(1.0); // Przykładowe wyłączenie/uszkodzenie
        
        Console.WriteLine("ALERT: AUTOPILOT DISENGAGED. MANUAL CONTROL ONLY.");
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // NAJWAŻNIEJSZY PUNKT: Sprawdzanie warunku przegranej z DamageModel
        if (ctx.DamageModel.CheckGameOver())
        {
            // Trigger sekwencji Game Over dla czarnej skrzynki
            EventBus.Instance.Publish(new GameOverEvent
            {
                Timestamp = DateTime.Now,
                Source = "DamageModel",
                Level = Core.Aircraft.Enums.Severity.Critical,
                Message = "FATAL: Aircraft destroyed",
                Reason = ctx.DamageModel.GameOverReason
            });
            
            // Pętla gry prawdopodobnie wyłapie ten event (np. w FlightController lub BlackBoxHandler) 
            // i zakończy działanie symulatora wyświetlając ekran Game Over
        }
        else
        {
            // Oddanie bardzo ograniczonego sterowania (np. duże znoszenie, utrudniona kontrola pitch/roll)
            if (ctx.DamageModel.AsymmetricDragActive)
            {
                ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec * 3.0, deltaT); 
            }
        }
    }

    public void Land(Aircraft ctx)
    {
        // Ostatnia szansa: awaryjne lądowanie (często crash landing)
        ctx.TransitionTo(new LandingState());
    }

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void HandleEmergency(Aircraft ctx) { } // Jesteśmy już w najgorszym stanie
    public void Abort(Aircraft ctx) { }

    public void OnExit(Aircraft ctx) { }
}