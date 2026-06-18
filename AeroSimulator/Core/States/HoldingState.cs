namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class HoldingState : IAircraftState
{
    public string StateName => "HOLDING";
    public string StateDescription => "Standard racetrack holding pattern due to traffic or weather.";
    public ConsoleColor StateColor => ConsoleColor.Magenta;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        ctx.FlightData.FuelLevelKg -= (ctx.Config.Aircraft.FuelBurnKgPerH / 3600.0) * deltaT;
        
        double fuelPercent = (ctx.FlightData.FuelLevelKg / ctx.Config.Aircraft.MaxFuelKg) * 100.0;
        if (fuelPercent < 5.0)
        {
            ctx.TransitionTo(new DescentState());
        }
    }

    public void Land(Aircraft ctx)
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

    public void Descend(Aircraft ctx)
    {
    }

    public void Abort(Aircraft ctx)
    {
    }

    public void OnExit(Aircraft ctx)
    {
    }
}