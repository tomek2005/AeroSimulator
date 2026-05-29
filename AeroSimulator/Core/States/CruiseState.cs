namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class CruiseState : IAircraftState
{
    public string StateName => "CRUISE";
    public string StateDescription => "Lot przelotowy z włączonym autopilotem.";
    public ConsoleColor StateColor => ConsoleColor.Blue;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Descend", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Włączenie autopilota (tryb ALT_HOLD + HDG)
        // Zakładając dostęp do systemu autopilota, np. ctx.GetSystem(SystemType.Autopilot)
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // W pełnej wersji spalanie paliwa robi ctx.FuelSystem.Burn(...), 
        // ale musimy sprawdzać poziom paliwa: Jeśli spadnie poniżej 5% -> Awaria
        if (ctx.FlightData.FuelRemainingPercent() < 5.0)
        {
            HandleEmergency(ctx);
            return;
        }

        // BARDZO WAŻNE: Obsługa asymetrycznego oporu (Asymmetric Drag) z DamageModel
        if (ctx.DamageModel.AsymmetricDragActive)
        {
            // Aplikujemy znoszenie samolotu (Heading drift) za pomocą dedykowanej metody we FlightData
            ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec, deltaT);
        }
    }

    public void Descend(Aircraft ctx) => ctx.TransitionTo(new DescentState());
    
    public void HandleEmergency(Aircraft ctx) => ctx.TransitionTo(new EmergencyState());

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }
    public void Abort(Aircraft ctx) { }
    public void OnExit(Aircraft ctx) { }
}