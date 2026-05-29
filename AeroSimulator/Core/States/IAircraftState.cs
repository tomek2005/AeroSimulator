namespace AeroSimulator.Core.States;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft;

/// <summary>
/// Interfejs reprezentujący abstrakcyjny stan samolotu w maszynie stanów.
/// </summary>
public interface IAircraftState
{
    // Właściwości stanu
    string StateName { get; }
    string StateDescription { get; }
    ConsoleColor StateColor { get; }
    IReadOnlyList<string> AllowedActions { get; }

    // Metody akcji gracza/kontrolera delegowane do stanu
    void TakeOff(Aircraft ctx);
    void Cruise(Aircraft ctx);
    void Descend(Aircraft ctx);
    void Land(Aircraft ctx);
    void HandleEmergency(Aircraft ctx);
    void Abort(Aircraft ctx);

    // Metody cyklu życia stanu
    void Update(Aircraft ctx, double deltaT);
    void OnEnter(Aircraft ctx);
    void OnExit(Aircraft ctx);
}