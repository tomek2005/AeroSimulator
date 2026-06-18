namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Aircraft.Enums;

public class EmergencyState : IAircraftState
{
    public string StateName => "EMERGENCY";
    public string StateDescription => "Emergency declared. High priority handling and troubleshooting.";
    public ConsoleColor StateColor => ConsoleColor.DarkRed;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Land", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        ctx.Publish(new MaydayEvent(EmergencyType.General, "Crew", "MAYDAY MAYDAY MAYDAY — Emergency state declared."));
        ctx.PublishAlert("MAYDAY, MAYDAY, MAYDAY! Emergency state declared.", Severity.Critical);
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        if (ctx.DamageModel.IsExploded || ctx.DamageModel.WingHealth < 0.25)
        {
            HandleEmergency(ctx);
        }
    }

    public void Land(Aircraft ctx)
    {
        ctx.TransitionTo(new LandingState());
    }

    public void HandleEmergency(Aircraft ctx)
    {
        ctx.TransitionTo(new CriticalState());
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