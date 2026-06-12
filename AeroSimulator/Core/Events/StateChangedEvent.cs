namespace AeroSimulator.Core.Events;

public class StateChangedEvent : FlightEvent
{
    public string OldState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
}