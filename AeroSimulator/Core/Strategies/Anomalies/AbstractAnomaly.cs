using AeroSim.Core.Aircraft;
using AeroSim.Core.Aircraft.Enums;
using AeroSim.Core.Events;

namespace AeroSim.Core.Strategies.Anomalies;

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
    public void Trigger(Aircraft.Aircraft ctx, FlightData data)
    {
        if (_isActive) return;          // guard against double-trigger
        _isActive = true;
        _activeDuration = 0;
        OnTrigger(ctx, data);
        PublishAlert(ctx, GetWarningMessage(), Level);
    }

    /// <inheritdoc/>
    public void Update(Aircraft.Aircraft ctx, FlightData data, double deltaT)
    {
        if (!_isActive) return;
        _activeDuration += deltaT;
        OnUpdate(ctx, data, deltaT);
    }

    /// <inheritdoc/>
    public bool Resolve(Aircraft.Aircraft ctx)
    {
        if (!CanBeResolved) return false;
        bool success = OnResolve(ctx);
        if (success)
        {
            _isActive = false;
            ctx.Publish(new AnomalyResolvedEvent
            {
                Source  = AnomalyName,
                Level   = Severity.Low,
                Message = $"{AnomalyName} resolved successfully.",
                Anomaly = this,
                Success = true
            });
        }
        else
        {
            ctx.Publish(new AnomalyResolvedEvent
            {
                Source  = AnomalyName,
                Level   = Level,
                Message = $"{AnomalyName} resolution FAILED.",
                Anomaly = this,
                Success = false
            });
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
    protected abstract void OnTrigger(Aircraft.Aircraft ctx, FlightData data);

    /// <summary>
    /// Called every tick while active. Advance timers, apply decay, check cascades.
    /// </summary>
    protected abstract void OnUpdate(Aircraft.Aircraft ctx, FlightData data, double deltaT);

    /// <summary>
    /// Attempts to resolve the anomaly. Return true on success, false on failure.
    /// Do NOT set <see cref="_isActive"/> here — the base class handles that.
    /// </summary>
    protected abstract bool OnResolve(Aircraft.Aircraft ctx);

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
    /// Publishes an <see cref="AnomalyTriggeredEvent"/> for the given cascade
    /// anomaly and then calls <see cref="AnomalyEngine"/> via the aircraft to
    /// force-spawn it so it starts affecting the simulation immediately.
    /// </summary>
    protected void TriggerCascade(Aircraft.Aircraft ctx, IAnomaly cascade)
    {
        ctx.Publish(new CascadeTriggeredEvent
        {
            Source  = AnomalyName,
            Level   = Severity.Critical,
            Message = $"CASCADE: {AnomalyName} → {cascade.AnomalyName}",
            SourceAnomaly = AnomalyName,
            TargetAnomaly = cascade.AnomalyName
        });

        // ForceSpawn bypasses the probability check — cascade anomalies are
        // always injected directly by the cascade system.
        ctx.ForceSpawnAnomaly(cascade);
    }

    /// <summary>
    /// Publishes a generic alert event and adds it to the alert bar.
    /// </summary>
    protected void PublishAlert(Aircraft.Aircraft ctx, string message, Severity level)
    {
        ctx.Publish(new AnomalyTriggeredEvent
        {
            Source  = AnomalyName,
            Level   = level,
            Message = message,
            Anomaly = this
        });
    }

    /// <summary>
    /// Deactivates this anomaly silently (used for self-resolving one-shot events
    /// like BirdStrike after their duration expires).
    /// </summary>
    protected void SelfResolve()
    {
        _isActive = false;
    }
}