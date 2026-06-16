using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events;

// Zamiana 'class' na 'record'
public record PlayerInputEvent : FlightEvent
{
    public PlayerAction Action { get; init; }
    public string KeyInfo { get; init; } = string.Empty;

    // Przekazanie wspólnych danych do bazowego, niemutowalnego konstruktora
    public PlayerInputEvent(PlayerAction action, string keyInfo, string message)
        : base(message, "InputHandler", Severity.Info)
    {
        Action = action;
        KeyInfo = keyInfo;
    }
}