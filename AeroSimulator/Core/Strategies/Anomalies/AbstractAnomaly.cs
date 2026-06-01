using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Abstract base class for all anomalies. Provides shared infrastructure:
/// probability checks, cascade chaining, alert publishing, and duration tracking.
/// Concrete anomalies override <see cref="OnTrigger"/>, <see cref="OnUpdate"/>,
/// and <see cref="OnResolve"/> rather than the public interface methods.
/// </summary>
public abstract class AbstractAnomaly : IAnomaly
{
    // ─── Fields ───────────────────────────────────────────────────────────────

    protected readonly Random _rng = new();

    /// <summary>How many seconds this anomaly has been active.</summary>
    protected double _activeDuration;

    /// <summary>Backing field for <see cref="IsActive"/>.</summary>
    protected bool _isActive;

    // ─── IAnomaly — public surface ────────────────────────────────────────────

    /// <inheritdoc/>
    public abstract string AnomalyName { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public abstract Severity Level { get; }

    /// <inheritdoc/>
    public abstract double Probability { get; }

    /// <inheritdoc/>
    public bool IsActive => _isActive;

    /// <inheritdoc/>
    public abstract bool CanBeResolved { get; }

    /// <inheritdoc/>
    public void Trigger(Aircraft ctx, FlightData data)
    {
        if (_isActive) return;
        _isActive = true;
        _activeDuration = 0;
        OnTrigger(ctx, data);
        PublishAlert(ctx, GetWarningMessage(), Level);
    }

    /// <inheritdoc/>
    public void Update(Aircraft ctx, FlightData data, double deltaT)
    {
        if (!_isActive) return;
        _activeDuration += deltaT;
        OnUpdate(ctx, data, deltaT);
    }

    /// <inheritdoc/>
    public bool Resolve(Aircraft ctx)
    {
        if (!CanBeResolved) return false;
        bool success = OnResolve(ctx);
        if (success)
        {
            _isActive = false;
        }
        return success;
    }

    /// <inheritdoc/>
    public abstract string GetWarningMessage();

    /// <inheritdoc/>
    public abstract string GetPilotAction();

    // ─── Template methods — override in concrete classes ──────────────────────

    /// <summary>
    /// Applies immediate effects when the anomaly first triggers.
    /// Called once by <see cref="Trigger"/> after guards pass.
    /// </summary>
    protected abstract void OnTrigger(Aircraft ctx, FlightData data);

    /// <summary>
    /// Called every tick while active. Advance timers, apply decay, check cascades.
    /// </summary>
    protected abstract void OnUpdate(Aircraft ctx, FlightData data, double deltaT);

    /// <summary>
    /// Attempts to resolve the anomaly. Return true on success, false on failure.
    /// Do NOT set <see cref="_isActive"/> here — the base class handles that.
    /// </summary>
    protected abstract bool OnResolve(Aircraft ctx);

    // ─── Protected helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true with probability <paramref name="chancePerSecond"/> scaled
    /// by <paramref name="deltaT"/>. Use for random per-tick dice rolls.
    /// </summary>
    protected bool CheckProbability(double chancePerSecond, double deltaT)
        => _rng.NextDouble() < chancePerSecond * deltaT;

    /// <summary>
    /// Returns true with a flat probability (not time-scaled).
    /// Use for one-off rolls (e.g. cascade trigger rolls).
    /// </summary>
    protected bool RollChance(double chance)
        => _rng.NextDouble() < chance;

    /// <summary>
    /// Chains a cascade anomaly: logs it and force-spawns it via the aircraft.
    /// </summary>
    protected void TriggerCascade(Aircraft ctx, IAnomaly cascade)
    {
        ctx.ForceSpawnAnomaly(cascade);
    }

    /// <summary>
    /// Publishes a generic alert via the aircraft (no-op until Aircraft.Publish is implemented).
    /// </summary>
    protected void PublishAlert(Aircraft ctx, string message, Severity level)
    {
        ctx.PublishAlert(message, level);
    }

    /// <summary>
    /// Deactivates this anomaly silently (used for self-resolving one-shot events).
    /// </summary>
    protected void SelfResolve()
    {
        _isActive = false;
    }
}