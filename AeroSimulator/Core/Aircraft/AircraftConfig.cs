namespace AeroSimulator.Core.Aircraft;


// Immutable specification for a specific aircraft type.
public record AircraftConfig
{
    public string DisplayName { get; init; } = string.Empty;
    public string TailNumber { get; init; } = string.Empty;
    public double MaxFuelKg { get; init; }
    public double MaxAltitudeFt { get; init; }
    public double CruiseSpeedKts { get; init; }
    public double MaxSpeedKts { get; init; }
    public double StallSpeedKts { get; init; }
    public double StallSpeedFlaps { get; init; }
    public int EngineCount { get; init; }
    public double MaxThrustKN { get; init; }
    public double MaxClimbRateFtMin { get; init; }
    public double NormalDescentFtMin { get; init; }
    public double V1SpeedKts { get; init; }
    public double VRSpeedKts { get; init; }
    public double V2SpeedKts { get; init; }
    public double MaxCrosswindKts { get; init; }
    public double FuelBurnKgPerH { get; init; }
    public double WingStrength { get; init; }
}