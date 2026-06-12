namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class CruiseState : IAircraftState
{
    public string StateName => "CRUISE";
    public string StateDescription => "Steady cruise under autopilot control.";
    public ConsoleColor StateColor => ConsoleColor.Green;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Descend", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Załączenie autopilota (Hold Altitude & Heading)
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Zużywanie paliwa w locie przelotowym
        ctx.FlightData.FuelLevelKg -= (ctx.Config.Aircraft.FuelBurnKgPerH / 3600.0) * deltaT;

        // Aplikowanie asymetrycznego oporu ze wspólnego DamageModel
        if (ctx.DamageModel.AsymmetricDragActive)
        {
            ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec, deltaT);
        }

        // Sprawdzenie krytycznego poziomu paliwa (< 5%) -> Automatyczny Emergency
        double fuelPercent = (ctx.FlightData.FuelLevelKg / ctx.Config.Aircraft.MaxFuelKg) * 100.0;
        if (fuelPercent < 5.0)
        {
            HandleEmergency(ctx);
        }
    }

    public void Descend(Aircraft ctx)
    {
        ctx.TransitionTo(new DescentState());
    }

    public void HandleEmergency(Aircraft ctx)
    {
        ctx.TransitionTo(new EmergencyState());
    }

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }
    public void Abort(Aircraft ctx) { }
    public void OnExit(Aircraft ctx) { }
}