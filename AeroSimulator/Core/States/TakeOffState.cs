

namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class TakeOffState : IAircraftState
{
    public string StateName => "TAKEOFF";
    public string StateDescription => "Full throttle takeoff run on the runway.";
    public ConsoleColor StateColor => ConsoleColor.Yellow;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Abort", "HandleEmergency" };

    private bool _hasRotated;

    public void OnEnter(Aircraft ctx)
    {
        ctx.FlightData.Throttle = 1.0;
        _hasRotated = false;
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        ctx.FlightData.Speed += 25.0 * deltaT; 

        if (ctx.FlightData.Speed >= ctx.Config.Aircraft.VRSpeedKts && !_hasRotated)
        {
            ctx.FlightData.PitchAngleDeg = 7.5;
            _hasRotated = true;
        }

        if (ctx.FlightData.Speed >= ctx.Config.Aircraft.V2SpeedKts)
        {
            ctx.FlightData.VerticalSpeed = ctx.Config.Aircraft.MaxClimbRateFtMin;
            ctx.FlightData.Altitude += (ctx.FlightData.VerticalSpeed / 60.0) * deltaT;
        }

        // Auto-przejście do wznoszenia po 1500 ftqqqq
        if (ctx.FlightData.Altitude >= 1500.0)
        {
            ctx.TransitionTo(new ClimbState());
        }
    }

    public void Abort(Aircraft ctx)
    {
        // Bezpieczne przerwanie startu tylko poniżej V1
        if (ctx.FlightData.Speed < ctx.Config.Aircraft.V1SpeedKts)
        {
            ctx.FlightData.Throttle = 0.0;
            ctx.TransitionTo(new GroundState());
        }
    }

    public void HandleEmergency(Aircraft ctx)
    {
        if (ctx.FlightData.Speed > ctx.Config.Aircraft.V1SpeedKts)
            ctx.TransitionTo(new EmergencyState());
        else
            Abort(ctx); // Poniżej V1 awaryjnie hamujemy
    }

    public void TakeOff(Aircraft ctx) { }
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }
    public void OnExit(Aircraft ctx) { }
}