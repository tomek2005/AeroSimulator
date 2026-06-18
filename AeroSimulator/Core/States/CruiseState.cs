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
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        ctx.FlightData.FuelLevelKg -= (ctx.Config.Aircraft.FuelBurnKgPerH / 3600.0) * deltaT;
        
        if (ctx.DamageModel.AsymmetricDragActive)
        {
            ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec, deltaT);
        }
        
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

    public void TakeOff(Aircraft ctx)
    {
    }

    public void Cruise(Aircraft ctx)
    {
    }

    public void Land(Aircraft ctx) => Descend(ctx);

    public void Abort(Aircraft ctx)
    {
    }

    public void OnExit(Aircraft ctx)
    {
    }
}