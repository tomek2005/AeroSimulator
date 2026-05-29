namespace AeroSimulator.Core.Aircraft.Enums;

/// <summary>
/// Represents every distinct phase of a complete flight, from engine start
/// to post-landing shutdown. The State pattern uses these to drive transitions;
/// the view uses them for contextual labels and allowed-actions menus.
///
/// <para>
/// Landing sub-phases (Approach → Glideslope → Flare → Rollout) are included
/// here so the view can show fine-grained feedback during the approach without
/// needing a separate enum.
/// </para>
/// </summary>
public enum FlightPhase
{
    // ── Ground ───────────────────────────────────────────────────────────
    /// <summary>Parked at gate; engines cold.</summary>
    Parked,

    /// <summary>Taxiing to or from the runway.</summary>
    Taxi,

    // ── Departure ────────────────────────────────────────────────────────
    /// <summary>Takeoff roll — throttle up, accelerating on the runway.</summary>
    TakeoffRoll,

    /// <summary>Rotation — nose lifting, aircraft leaving the ground.</summary>
    Rotation,

    /// <summary>Initial climb — positive rate, gear coming up.</summary>
    InitialClimb,

    /// <summary>Climb to cruise altitude.</summary>
    Climb,

    // ── En-route ─────────────────────────────────────────────────────────
    /// <summary>Level cruise at target altitude and speed.</summary>
    Cruise,

    /// <summary>Holding pattern — racetrack orbit waiting for clearance.</summary>
    Holding,

    // ── Arrival ──────────────────────────────────────────────────────────
    /// <summary>Descending from cruise altitude toward the destination.</summary>
    Descent,

    /// <summary>Established on approach; ATC clearance received.</summary>
    Approach,

    /// <summary>On the ILS / visual glideslope, gear and flaps configured.</summary>
    Glideslope,

    /// <summary>Flare manoeuvre — nose up, reducing sink rate over runway threshold.</summary>
    Flare,

    /// <summary>Wheels on the ground, decelerating on the runway.</summary>
    Rollout,

    // ── Emergency ────────────────────────────────────────────────────────
    /// <summary>MAYDAY declared; priority handling, emergency procedures active.</summary>
    Emergency,

    /// <summary>
    /// Critical / unrecoverable situation — damage model is checking for game-over
    /// each tick. Limited player control.
    /// </summary>
    Critical
}