using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record LandingCompletedEvent : FlightEvent
{
    public bool GearExtended { get; init; }
    public double TouchdownSpeedKts { get; init; }
    public double TouchdownVerticalSpeedFtMin { get; init; }
    public double TouchdownPitchDeg { get; init; }

    public LandingCompletedEvent(
        bool gearExtended,
        double speedKts,
        double verticalSpeedFtMin,
        double pitchDeg,
        string message)
    {
        GearExtended = gearExtended;
        TouchdownSpeedKts = speedKts;
        TouchdownVerticalSpeedFtMin = verticalSpeedFtMin;
        TouchdownPitchDeg = pitchDeg;
        Source = "Landing";
        Level = Severity.Info;
        Message = message;
    }
}