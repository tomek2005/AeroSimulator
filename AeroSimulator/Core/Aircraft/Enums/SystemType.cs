namespace AeroSimulator.Core.Aircraft.Enums;

/// <summary>
/// Identifies a non-engine avionics system on the aircraft.
/// Engines are NOT listed here — they are addressed by their zero-based
/// <c>engineIndex</c> integer everywhere in the codebase, because an aircraft
/// can have 1, 2, 4, or more engines depending on the model.
/// </summary>
public enum SystemType
{
    Fuel,
    Navigation,
    Hydraulics,
    Electrical,
    Weather,
    Autopilot,
    Wing
}

