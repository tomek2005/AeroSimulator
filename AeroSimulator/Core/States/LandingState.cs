namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;


public class LandingState : IAircraftState
{
    public string StateName => "LANDING";
    public string StateDescription => "Final precision approach and touchdown sequence.";
    public ConsoleColor StateColor => ConsoleColor.DarkGreen;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Abort", "HandleEmergency" };

    public void OnEnter(Aircraft ctx) { }

    public void Update(Aircraft ctx, double deltaT)
    {
        ctx.FlightData.Altitude -= 18.0 * deltaT; 

        if (ctx.DamageModel.AsymmetricDragActive)
        {
            // Przy lądowaniu znoszenie jest bardziej odczuwalne
            ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec * 1.5, deltaT);
        }

        if (ctx.FlightData.Altitude <= 0)
        {
            ctx.FlightData.Altitude = 0;
            ctx.TransitionTo(new GroundState());
        }
    }

    public void Abort(Aircraft ctx)
    {
        ctx.FlightData.Throttle = 1.0;
        ctx.TransitionTo(new ClimbState());
    }

    public void HandleEmergency(Aircraft ctx) => ctx.TransitionTo(new EmergencyState());

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }
    public void OnExit(Aircraft ctx) { }
}