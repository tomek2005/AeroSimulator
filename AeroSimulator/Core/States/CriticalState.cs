using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Events;

public class CriticalState : IAircraftState
{
    public string StateName => "CRITICAL";
    public string StateDescription => "CRITICAL SYSTEMS FAILURE. Immediate action required to survive.";
    public ConsoleColor StateColor => ConsoleColor.Red;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land" };

    public void OnEnter(Aircraft ctx)
    {
        if (!ctx.AutopilotSystem.IsOffline)
        {
            ctx.AutopilotSystem.SetOffline();
        }

        ctx.PublishAlert("CRITICAL: FLIGHT ENVELOPE COMPROMISED. MANUAL CONTROL ONLY.", Severity.Critical);
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        ctx.FlightData.VerticalSpeed -= 500.0 * deltaT;
        ctx.FlightData.Speed -= 15.0 * deltaT;
        
        if (ctx.DamageModel.AsymmetricDragActive)
        {
            ctx.FlightData.ApplyAsymmetricDrift(ctx.DamageModel.DriftDegPerSec * 4.0, deltaT);
        }

        if (ctx.DamageModel.IsGameOver)
        {
            ctx.PublishAlert("FATAL IMPACT IMMINENT. SYSTEM TERMINATION.", Severity.Critical);
        }
    }

    public void Land(Aircraft ctx)
    {
        ctx.TransitionTo(new LandingState());
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