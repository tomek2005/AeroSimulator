namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class ClimbState : IAircraftState
{
    public string StateName => "CLIMB";
    public string StateDescription => "Climbing to assigned cruise altitude.";
    public ConsoleColor StateColor => ConsoleColor.Cyan;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Cruise", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Chowanie podwozia i klap
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        ctx.FlightData.Altitude += (ctx.Config.Aircraft.MaxClimbRateFtMin / 60.0) * deltaT;

        // Osiągnięcie docelowego pułapu przelotowego
        if (ctx.FlightData.Altitude >= ctx.Config.Aircraft.MaxAltitudeFt)
        {
            ctx.TransitionTo(new CruiseState());
        }
    }

    public void Cruise(Aircraft ctx)
    {
        ctx.TransitionTo(new CruiseState());
    }

    public void HandleEmergency(Aircraft ctx)
    {
        ctx.TransitionTo(new EmergencyState());
    }

    public void TakeOff(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }
    public void Abort(Aircraft ctx) { }
    public void OnExit(Aircraft ctx) { }
}