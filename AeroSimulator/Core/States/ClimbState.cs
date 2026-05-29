namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

public class ClimbState : IAircraftState
{
    private double _targetAltitude;
    private double _climbRate;

    public string StateName => "CLIMB";
    public string StateDescription => "Wznoszenie na wysokość przelotową.";
    public ConsoleColor StateColor => ConsoleColor.Cyan;
    public IReadOnlyList<string> AllowedActions => new List<string> { "Cruise", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Chowanie podwozia i klap
        // W pełnej wersji systemu może to wymagać wywołania na: ctx.Hydraulics
        
        // Pobieramy docelową wysokość i prędkość wznoszenia z konfiguracji lub telemetryki
        _targetAltitude = ctx.FlightData.TargetAltitude > 0 ? ctx.FlightData.TargetAltitude : ctx.Config.MaxAltitudeFt;
        _climbRate = ctx.Config.MaxClimbRateFtMin / 60.0; // konwersja na stopy na sekundę
    }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Zwiększanie wysokości
        ctx.FlightData.Altitude += _climbRate * deltaT;

        // Kiedy osiągniemy pułap -> Przejście do lotu przelotowego
        if (ctx.FlightData.Altitude >= _targetAltitude)
        {
            ctx.FlightData.Altitude = _targetAltitude; // Wyrównanie dla pewności
            ctx.TransitionTo(new CruiseState());
        }
    }

    public void Cruise(Aircraft ctx) => ctx.TransitionTo(new CruiseState());
    
    public void HandleEmergency(Aircraft ctx) => ctx.TransitionTo(new EmergencyState());

    public void TakeOff(Aircraft ctx) { }
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) { }
    public void Abort(Aircraft ctx) { }
    public void OnExit(Aircraft ctx) { }
}