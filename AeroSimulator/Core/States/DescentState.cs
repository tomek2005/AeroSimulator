namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class DescentState : IAircraftState
{
    public string StateName => "DESCENT";
    public string StateDescription => "Descending towards the destination airport.";
    public ConsoleColor StateColor => ConsoleColor.Blue;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land", "Abort", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        ctx.FlightData.Throttle = 0.3;
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        ctx.FlightData.Altitude -= (ctx.Config.Aircraft.NormalDescentFtMin / 60.0) * deltaT;
        ctx.FlightData.Speed -= 4.0 * deltaT;

        if (ctx.FlightData.Altitude <= 3000.0 && ctx.FlightData.Speed < 180.0)
        {
            Land(ctx);
        }
    }

    public void Land(Aircraft ctx) => ctx.TransitionTo(new LandingState());

    public void Abort(Aircraft ctx) => ctx.TransitionTo(new ClimbState());

    public void HandleEmergency(Aircraft ctx) => ctx.TransitionTo(new EmergencyState());
    
    public void TakeOff(Aircraft ctx)
    {
    }

    public void Cruise(Aircraft ctx)
    {
    }

    public void Descend(Aircraft ctx)
    {
    } 

    public void OnExit(Aircraft ctx)
    {
    }
}