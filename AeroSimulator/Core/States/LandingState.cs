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

    public void OnEnter(Aircraft ctx)
    {
        ctx.FlightData.Throttle = Math.Min(ctx.FlightData.Throttle, 0.25);
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        double descentRateFtMin = ctx.HydraulicSystem.IsGearExtended ? 520.0 : 220.0;
        ctx.FlightData.VerticalSpeed = -descentRateFtMin;
        ctx.FlightData.Altitude -= (descentRateFtMin / 60.0) * deltaT;
        ctx.FlightData.Speed = Math.Max(0.0,
            ctx.FlightData.Speed - (ctx.HydraulicSystem.IsGearExtended ? 5.0 : 8.0) * deltaT);

        if (ctx.DamageModel.AsymmetricDragActive)
        {
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
        ctx.FlightData.PitchAngleDeg = 8.0;
        ctx.TransitionTo(new ClimbState());
    }

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

    public void Land(Aircraft ctx)
    {
    }

    public void OnExit(Aircraft ctx)
    {
    }
}