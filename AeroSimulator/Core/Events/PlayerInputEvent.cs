using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

public record PlayerInputEvent : FlightEvent
{
    public PlayerAction Action { get; init; }
    public string KeyInfo { get; init; } = string.Empty;
    
    public PlayerInputEvent(PlayerAction action, string keyInfo, string message)
        : base(message, "InputHandler", Severity.Info)
    {
        Action = action;
        KeyInfo = keyInfo;
    }
}