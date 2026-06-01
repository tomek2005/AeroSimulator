using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Defines the contract for all flight anomalies that can occur during simulation.
/// Anomalies are the Strategy pattern implementations — each encapsulates a specific
/// failure scenario with its own trigger logic, update behavior, and resolution path.
/// </summary>
public interface IAnomaly
{
    /// <summary>Display name shown in alerts and black box log.</summary>
    string AnomalyName { get; }

    /// <summary>Short one-line description shown in the alert bar.</summary>
    string Description { get; }

    /// <summary>Severity level: Low / Medium / High / Critical.</summary>
    Severity Level { get; }

    /// <summary>Base probability of this anomaly triggering per second (0.0–1.0).</summary>
    double Probability { get; }

    /// <summary>True while the anomaly is actively affecting the aircraft.</summary>
    bool IsActive { get; }

    /// <summary>True if the player can resolve this anomaly via [R] key.</summary>
    bool CanBeResolved { get; }

    /// <summary>
    /// Activates the anomaly: applies immediate effects to the aircraft and
    /// publishes relevant events to the EventBus.
    /// </summary>
    void Trigger(Aircraft ctx, FlightData data);

    /// <summary>
    /// Called every simulation tick while <see cref="IsActive"/> is true.
    /// Applies ongoing damage, checks cascade conditions, advances timers.
    /// </summary>
    void Update(Aircraft ctx, FlightData data, double deltaT);

    /// <summary>
    /// Attempts player-initiated resolution. Returns true on success.
    /// Sets <see cref="IsActive"/> = false on success.
    /// </summary>
    bool Resolve(Aircraft ctx);

    /// <summary>Short warning message for the dashboard alert bar.</summary>
    string GetWarningMessage();

    /// <summary>Instruction displayed to the player on how to handle this anomaly.</summary>
    string GetPilotAction();
}