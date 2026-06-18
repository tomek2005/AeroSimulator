using AeroSimulator.Core.Aircraft;

namespace AeroSimulator.Infrastructure;

/// Statyczna baza niezmiennych danych wejściowych (samoloty i trasy).
public static class DataPresets
{
    public static readonly IReadOnlyList<AircraftConfig> AircraftPresets = new List<AircraftConfig>
    {
        new()
        {
            DisplayName = "Boeing 737-800", TailNumber = "SP-LRA", EngineCount = 2, MaxFuelKg = 20890,
            MaxAltitudeFt = 41000, CruiseSpeedKts = 461, MaxSpeedKts = 515, StallSpeedKts = 130, StallSpeedFlaps = 112,
            MaxThrustKN = 242, MaxClimbRateFtMin = 3000, NormalDescentFtMin = 1800, V1SpeedKts = 145, VRSpeedKts = 150,
            V2SpeedKts = 155, MaxCrosswindKts = 35, FuelBurnKgPerH = 2500, WingStrength = 0.8
        },
        new()
        {
            DisplayName = "Airbus A320", TailNumber = "SP-LRE", EngineCount = 2, MaxFuelKg = 19000,
            MaxAltitudeFt = 39800, CruiseSpeedKts = 454, MaxSpeedKts = 500, StallSpeedKts = 125, StallSpeedFlaps = 108,
            MaxThrustKN = 240, MaxClimbRateFtMin = 3200, NormalDescentFtMin = 1800, V1SpeedKts = 140, VRSpeedKts = 145,
            V2SpeedKts = 150, MaxCrosswindKts = 38, FuelBurnKgPerH = 2400, WingStrength = 0.85
        },
        new()
        {
            DisplayName = "Cessna 172 Skyhawk", TailNumber = "SP-YOL", EngineCount = 1, MaxFuelKg = 144,
            MaxAltitudeFt = 14000, CruiseSpeedKts = 122, MaxSpeedKts = 163, StallSpeedKts = 53, StallSpeedFlaps = 48,
            MaxThrustKN = 1.2, MaxClimbRateFtMin = 730, NormalDescentFtMin = 500, V1SpeedKts = 50, VRSpeedKts = 55,
            V2SpeedKts = 60, MaxCrosswindKts = 15, FuelBurnKgPerH = 35, WingStrength = 0.5
        }
    }.AsReadOnly();

    public static readonly IReadOnlyList<RouteConfig> RoutePresets = new List<RouteConfig>
    {
        new("Szkoleniowa (Traffic Pattern)", "EPWA (Start i lądowanie na tym samym pasie)", 15, "3 minuty"),

        new("Krótka (Short)", "EPWA (Warszawa) ➔ EPKK (Kraków)", 250, "30 min"),
        new("Średnia (Medium)", "EPWA (Warszawa) ➔ EDDB (Berlin)", 520, "1 godz 00 min"),
        new("Długa (Long)", "EPWA (Warszawa) ➔ EGLL (Londyn)", 1450, "2 godz 15 min")
    }.AsReadOnly();
}