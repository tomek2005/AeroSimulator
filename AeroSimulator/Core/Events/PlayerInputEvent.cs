using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public class PlayerInputEvent : FlightEvent
{
    public string Key { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;

    public PlayerInputEvent(string key, string action, string message)
    {
        Key = key;
        Action = action;
        Source = "PlayerInput";
        Level = Severity.Info;
        Message = message;
    }
}