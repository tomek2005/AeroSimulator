namespace AeroSimulator.Core.Aircraft.Enums;


// Represents every distinct phase of a complete flight, from engine start to post-landing shutdown. 
public enum FlightPhase
{
    Parked,
    Taxi,
    TakeoffRoll,
    Rotation,
    InitialClimb,
    Climb,
    Cruise,
    Holding,
    Descent,
    Approach,
    Glideslope,
    Flare,
    Rollout,
    Emergency,
    Critical
}