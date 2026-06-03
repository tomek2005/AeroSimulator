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
    // ── Identity ──────────────────────────────────────────────────────────

    /// <summary>Human-readable model name.</summary>
    /// <value>Example: "Boeing 737-800" or "Cessna 172".</value>
    public required string DisplayName { get; init; }

    /// <summary>Default registration tail number.</summary>
    /// <value>Example: "SP-LRA".</value>
    public required string TailNumber { get; init; }

    // ── Engines ──────────────────────────────────────────────────────────

    /// <summary>Number of engines on the aircraft.</summary>
    /// <value>Used to initialize engine systems, sensors, and flight data arrays.</value>
    public int NumberOfEngines { get; init; } = 2;

    // ── Fuel ──────────────────────────────────────────────────────────────

    /// <summary>Maximum fuel capacity.</summary>
    /// <value>The total mass of fuel the aircraft can carry, measured in kilograms.</value>
    public required double MaxFuelKg { get; init; }

    /// <summary>Normal cruise fuel burn rate.</summary>
    /// <value>The combined fuel consumption of all engines during cruise, in kilograms per hour.</value>
    public required double FuelBurnKgPerH { get; init; }

    // ── Performance envelope ──────────────────────────────────────────────

    /// <summary>Certified service ceiling.</summary>
    /// <value>Maximum operating altitude in feet Mean Sea Level (MSL).</value>
    public required double MaxAltitudeFt { get; init; }

    /// <summary>Normal cruise speed.</summary>
    /// <value>Target indicated airspeed (IAS) during cruise phase, in knots.</value>
    public required double CruiseSpeedKts { get; init; }

    /// <summary>Maximum operating speed (VMO).</summary>
    /// <value>The structural speed limit in knots IAS. Exceeding this triggers overspeed warnings.</value>
    public required double MaxSpeedKts { get; init; }

    /// <summary>Stall speed in a clean configuration (VS1).</summary>
    /// <value>Minimum flying speed in knots IAS with flaps and gear retracted.</value>
    public required double StallSpeedKts { get; init; }

    /// <summary>Stall speed in a landing configuration (VS0).</summary>
    /// <value>Minimum flying speed in knots IAS with full flaps extended.</value>
    public required double StallSpeedFlaps { get; init; }

    // ── Engines ───────────────────────────────────────────────────────────

    /// <summary>Number of engines equipped on the aircraft.</summary>
    /// <value>An integer from 1 upwards (e.g., 2 for a B737, 4 for a B747). Drives the dynamic array allocation in <see cref="FlightData"/>.</value>
    public required int EngineCount { get; init; }

    /// <summary>Maximum thrust generated per engine.</summary>
    /// <value>Force measured in kilonewtons (kN).</value>
    public required double MaxThrustKN { get; init; }

    // ── Climb / descent ───────────────────────────────────────────────────

    /// <summary>Maximum sustained climb rate.</summary>
    /// <value>Vertical speed in feet per minute (ft/min).</value>
    public required double MaxClimbRateFtMin { get; init; }

    /// <summary>Normal (idle) descent rate.</summary>
    /// <value>Positive value representing descent speed in feet per minute (ft/min).</value>
    public required double NormalDescentFtMin { get; init; }

    // ── V-speeds ──────────────────────────────────────────────────────────

    /// <summary>Decision speed (V1).</summary>
    /// <value>Speed in knots beyond which the takeoff must continue even if an engine fails.</value>
    public required double V1SpeedKts { get; init; }

    /// <summary>Rotation speed (VR).</summary>
    /// <value>Speed in knots at which the pilot initiates pitch-up for liftoff.</value>
    public required double VRSpeedKts { get; init; }

    /// <summary>Takeoff safety speed (V2).</summary>
    /// <value>Target speed in knots to be reached at 35 feet above the runway after an engine failure.</value>
    public required double V2SpeedKts { get; init; }

    // ── Handling ──────────────────────────────────────────────────────────

    /// <summary>Maximum demonstrated crosswind component.</summary>
    /// <value>Limit for safe landings with wind perpendicular to the runway, in knots.</value>
    public required double MaxCrosswindKts { get; init; }

    // ── Structural ────────────────────────────────────────────────────────

    /// <summary>Wing structural resilience factor.</summary>
    /// <value>
    /// A scale from 0.5 to 1.0. Higher values indicate slower melt rates during fire emergencies.
    /// Used to calculate damage accumulation over time.
    /// </value>
    public required double WingStrength { get; init; }
}