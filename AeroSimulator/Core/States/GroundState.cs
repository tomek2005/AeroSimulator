namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

public class GroundState : IAircraftState
{
    // Prywatne pola wymagane przez specyfikację
    private bool _isEngineRunning = false;
    private int _gateNumber = 1;
    private double _refuelRate = 50.0;

    public string StateName => "GROUND";
    public string StateDescription => "Samolot na ziemi, gotowy do boardingu i kołowania.";
    public ConsoleColor StateColor => ConsoleColor.DarkGray;
    public IReadOnlyList<string> AllowedActions => new List<string> { "TakeOff", "HandleEmergency" };

    public void OnEnter(Aircraft ctx)
    {
        // Ustawienia początkowe
        ctx.FlightData.Speed = 0;
        ctx.FlightData.VerticalSpeed = 0;
        ctx.FlightData.Throttle = 0;
        _isEngineRunning = false;
    }

    public void TakeOff(Aircraft ctx)
    {
        double fuelPct = ctx.FlightData.FuelRemainingPercent();
    
    // Zamiast starego GetSystemHealth, pobieramy zdrowie z pierwszego silnika (indeks 0)
    // (Komentarz w kodzie słusznie sugerował, żeby użyć go bezpośrednio!)
        double engineHealth = ctx.GetEngine(0).Health; 

        if (fuelPct > 10.0 && engineHealth > 0.3)
        {
            ctx.TransitionTo(new TaxiState());
        }
        else
        {
            Console.WriteLine("ALERT: Check fuel and engine health before taxiing!");
        }
    }

    public void Cruise(Aircraft ctx) => Console.WriteLine("ALERT: Cannot cruise on ground");
    public void Descend(Aircraft ctx) { }
    public void Land(Aircraft ctx) => Console.WriteLine("ALERT: Already on ground");
    public void HandleEmergency(Aircraft ctx) => Console.WriteLine("ALERT: Ground emergency");
    public void Abort(Aircraft ctx) { }

    public void Update(Aircraft ctx, double deltaT)
    {
        // Logika rozgrzewania silników lub tankowania z odpowiednią prędkością _refuelRate
    }

    public void OnExit(Aircraft ctx) { }
}