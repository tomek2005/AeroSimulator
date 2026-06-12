namespace AeroSimulator.Core.Aircraft;

/// <summary>
/// Immutable specification for a specific aircraft type.
/// </summary>
/// <remarks>
/// Created once by <see cref="AeroSim.Infrastructure.AircraftFactory"/> and
/// read throughout the simulation. All physical limits, capacities, and V-speeds come from here.
/// This configuration determines the size of engine arrays in <see cref="FlightData"/>.
/// </remarks>
public record AircraftConfig
{
    public string DisplayName       { get; init; } = string.Empty;
    public string TailNumber        { get; init; } = string.Empty;
    public double MaxFuelKg         { get; init; }
    public double MaxAltitudeFt     { get; init; }
    public double CruiseSpeedKts    { get; init; }
    public double MaxSpeedKts       { get; init; } // VMO
    public double StallSpeedKts     { get; init; } // clean
    public double StallSpeedFlaps   { get; init; } // flaps full
    public int    EngineCount       { get; init; }
    public double MaxThrustKN       { get; init; }
    public double MaxClimbRateFtMin { get; init; }
    public double NormalDescentFtMin{ get; init; }
    public double V1SpeedKts        { get; init; }
    public double VRSpeedKts        { get; init; }
    public double V2SpeedKts        { get; init; }
    public double MaxCrosswindKts   { get; init; }
    public double FuelBurnKgPerH    { get; init; } // at cruise
    public double WingStrength      { get; init; } // jak szybko topi się skrzydło (0.5-1.0)
}