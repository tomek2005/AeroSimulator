namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

/// <summary>
/// Interfejs bazowy wzorca Stanu (State Pattern).
/// Wszystkie akcje samolotu są delegowane do aktualnego stanu, eliminując instrukcje if/switch w Aircraft.
/// </summary>
public interface IAircraftState
{
    string StateName { get; }
    string StateDescription { get; }
    ConsoleColor StateColor { get; }
    IReadOnlyList<string> AllowedActions { get; }

    void TakeOff(Aircraft ctx);
    void Cruise(Aircraft ctx);
    void Descend(Aircraft ctx);
    void Land(Aircraft ctx);
    void HandleEmergency(Aircraft ctx);
    void Abort(Aircraft ctx);
    
    void Update(Aircraft ctx, double deltaT);
    void OnEnter(Aircraft ctx);
    void OnExit(Aircraft ctx);
}