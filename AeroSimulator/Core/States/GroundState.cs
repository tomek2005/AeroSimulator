namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

public class GroundState : IAircraftState
{
    public string StateName => "GROUND";
    public string StateDescription => "Aircraft is parked at the gate. Pre-flight checks.";
    public ConsoleColor StateColor => ConsoleColor.Gray;
    public IReadOnlyList<string> AllowedActions => new List<string> { "TakeOff" };

    public void OnEnter(Aircraft ctx)
    {
        ctx.FlightData.Speed = 0;
        ctx.FlightData.VerticalSpeed = 0;
    }

    public void Update(Aircraft ctx, double deltaT)
    {
    }

    public void TakeOff(Aircraft ctx)
    {
        for (int i = 0; i < ctx.EngineCount; i++)
        {
            if (ctx.DamageModel.GetEngineHealth(i) <= 0.3) return;
        }

        ctx.TransitionTo(new TaxiState());
    }

    public void Cruise(Aircraft ctx)
    {
    }

    public void Descend(Aircraft ctx)
    {
    }

    public void Land(Aircraft ctx)
    {
    }

    public void HandleEmergency(Aircraft ctx)
    {
    }

    public void Abort(Aircraft ctx)
    {
    }

    public void OnExit(Aircraft ctx)
    {
    }
}