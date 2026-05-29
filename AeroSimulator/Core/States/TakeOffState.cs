namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class TakeOffState : IAircraftState
{
    // Pola prywatne
    private double _v1Speed;
    private double _vrSpeed;
    private bool _hasRotated;

    public string StateName => "TAKE OFF";
    public string StateDescription => "Rozbieg na pasie startowym.";
    public ConsoleColor StateColor => ConsoleColor.Green;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Abort", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Odczyt z AircraftConfig
        _v1Speed = ctx.Config.V1SpeedKts;
        _vrSpeed = ctx.Config.VRSpeedKts;
        
        ctx.FlightData.Throttle = 1.0; // Przepustnica na max
        _hasRotated = false;
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Symulacja przyspieszenia z pełną przepustnicą
        ctx.FlightData.Speed += (ctx.FlightData.Throttle * 10.0) * deltaT; 

        // Rotacja: przy prędkości VR podnosimy nos
        if (ctx.FlightData.Speed >= _vrSpeed && !_hasRotated)
        {
            ctx.FlightData.PitchAngleDeg = 7.5;
            _hasRotated = true;
        }

        // Gdy już nos jest w górze, samolot zaczyna nabierać wysokości
        if (_hasRotated)
        {
            // Prędkość wznoszenia
            ctx.FlightData.Altitude += 80.0 * deltaT; 
        }

        // Przy 1500 ft przechodzimy w fazę Climbing
        if (ctx.FlightData.Altitude >= 1500.0)
        {
            ctx.TransitionTo(new ClimbState());
        }
    }

    public void Abort(Aircraft ctx)
    {
        // Jeśli jesteśmy poniżej prędkości decyzyjnej V1, możemy bezpiecznie przerwać
        if (ctx.FlightData.Speed < _v1Speed)
        {
            ctx.FlightData.Throttle = 0; // Odcięcie ciągu
            // Tutaj można by było aktywować hamulce Hydraulics
            ctx.TransitionTo(new GroundState());
        }
        else
        {
            Console.WriteLine("ALERT: Too fast to abort! Speed > V1.");
        }
    }

    public void HandleEmergency(Aircraft ctx)
    {
        // Decyzja na podstawie prędkości V1
        if (ctx.FlightData.Speed > _v1Speed)
        {
            // Za późno na hamowanie - ogłaszamy awarię, ale kontynuujemy lot
            ctx.TransitionTo(new EmergencyState());
        }
        else
        {
            // Jesteśmy przed V1, wyhamowujemy bezpiecznie na pasie
            Abort(ctx);
        }
    }

    public void TakeOff(Aircraft ctx) { } // Jesteśmy już w trakcie
    public void Cruise(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }

    public void OnExit(Aircraft ctx) { }
}