namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class TaxiState : IAircraftState
{
    public string StateName => "TAXI";
    public string StateDescription => "Taxiing to the active runway.";
    public ConsoleColor StateColor => ConsoleColor.DarkYellow;
    public IReadOnlyList<string> AllowedActions => new List<string> { "TakeOff", "Abort" };

    public void OnEnter(Aircraft ctx)
    {
        ctx.FlightData.Speed = 15.0;
    }

    public void Update(Aircraft ctx, double deltaT)
    {
    }

    public void TakeOff(Aircraft ctx) => ctx.TransitionTo(new TakeOffState());

    public void Abort(Aircraft ctx) => ctx.TransitionTo(new GroundState());

    public void Cruise(Aircraft ctx)
    {
    }

    public void Descend(Aircraft ctx)
    {
    }

    public void Land(Aircraft ctx)
    {
    }

    public void HandleEmergency(Aircraft ctx) => ctx.TransitionTo(new EmergencyState());

    public void OnExit(Aircraft ctx)
    {
    }
}