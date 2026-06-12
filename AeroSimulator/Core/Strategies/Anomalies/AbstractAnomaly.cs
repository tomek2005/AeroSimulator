using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Events;

namespace AeroSimulator.Core.Strategies.Anomalies;


using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

/// <summary>
/// Abstract base class for all anomalies. Provides shared infrastructure:
/// probability checks, event publishing, and duration tracking.
/// </summary>
public abstract class AbstractAnomaly : IAnomaly
{
    protected readonly Random _rng = new();
    protected double _activeDuration;
    protected bool _isActive;

    public abstract string AnomalyName { get; }
    public abstract string Description { get; }
    public abstract Severity Level { get; }
    public abstract double Probability { get; }
    public bool IsActive => _isActive;
    public abstract bool CanBeResolved { get; }

    public void Trigger(Aircraft ctx, FlightData data)
    {
        if (_isActive) return;
        _isActive = true;
        _activeDuration = 0;
        OnTrigger(ctx, data);
        ctx.Publish(new AnomalyTriggeredEvent(AnomalyName, Level, GetWarningMessage()));
    }

    public void Update(Aircraft ctx, FlightData data, double deltaT)
    {
        if (!_isActive) return;
        _activeDuration += deltaT;
        OnUpdate(ctx, data, deltaT);
    }

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

    public abstract string GetWarningMessage();
    public abstract string GetPilotAction();

    protected abstract void OnTrigger(Aircraft ctx, FlightData data);
    protected abstract void OnUpdate(Aircraft ctx, FlightData data, double deltaT);
    protected abstract bool OnResolve(Aircraft ctx);

    protected bool CheckProbability(double chancePerSecond, double deltaT)
        => _rng.NextDouble() < chancePerSecond * deltaT;

    protected bool RollChance(double chance)
        => _rng.NextDouble() < chance;

    protected void PublishAlert(Aircraft ctx, string message, Severity level)
    {
        ctx.PublishAlert(message, level);
    }

    protected void SelfResolve()
    {
        _isActive = false;
    }
}
