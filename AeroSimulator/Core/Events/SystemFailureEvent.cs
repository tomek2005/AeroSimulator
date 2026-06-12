using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class SystemFailureEvent : FlightEvent
{
    public string SystemName { get; init; } = string.Empty;
    public double Health { get; init; }

    public SystemFailureEvent(string systemName, double health, string message) 
    {
        SystemName = systemName;
        Health = health;
        Source = "Systems";
        Level = Severity.High;
        Message = message;
    }
}