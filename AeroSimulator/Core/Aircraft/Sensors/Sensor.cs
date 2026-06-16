using System;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Common; // <-- DODANY IMPORT NASZEJ MONADY

namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Base sensor implementation with Gaussian-style noise, fault sticking,
/// and dead-return behaviour using the Option monad.
/// Subclasses override <see cref="Scale"/> to apply unit-specific noise scaling.
/// </summary>
public class Sensor : ISensor
{
    // ── Private state ─────────────────────────────────────────────────────

    private readonly Random _rng = new();

    /// <summary>Temporary noise boost added by e.g. turbulence or weather.</summary>
    private double _noiseBoost;

    /// <summary>Last value returned to callers; used as stuck value in Fault state.</summary>
    private double _lastReading;

    // ── ISensor implementation ────────────────────────────────────────────

    /// <inheritdoc/>
    public string SensorName { get; }

    /// <inheritdoc/>
    public SensorState State { get; private set; } = SensorState.OK;

    /// <inheritdoc/>
    public double Accuracy { get; private set; } = 1.0;

    /// <summary>
    /// Noise scale factor — how much of the real value can become noise.
    /// Default 0.15 (±15 %). Subclasses may override for tighter or looser sensors.
    /// </summary>
    protected virtual double Scale => 0.15;

    /// <param name="sensorName">Dashboard identifier, e.g. "ALT-SNS".</param>
    public Sensor(string sensorName)
    {
        SensorName    = sensorName;
        _lastReading  = 0;
    }

    /// <inheritdoc/>
    public Option<double> Read(double realValue) // <-- ZMIANA TYPU ZWRACANEGO NA OPTION
    {
        switch (State)
        {
            case SensorState.Dead:
                // Zamiast "-1.0" zwracamy funkcyjny brak wartości:
                return Option<double>.None();

            case SensorState.Fault:
                // Zwracamy "zamrożoną" (nieaktualną) wartość opakowaną w strukturyzowane Some:
                return Option<double>.Some(_lastReading);

            default:
                // Noisy or OK — add proportional Gaussian noise
                double totalNoiseLevel = (1.0 - Accuracy) + _noiseBoost;
                double noiseMagnitude  = realValue * Scale * totalNoiseLevel;
                double noiseSample     = (_rng.NextDouble() - 0.5) * 2.0 * noiseMagnitude;
                _lastReading = realValue + noiseSample;
                
                // Zwracamy przeliczoną wartość opakowaną w Some:
                return Option<double>.Some(_lastReading);
        }
    }

    /// <inheritdoc/>
    public void ApplyDamage(double severity)
    {
        severity = Math.Clamp(severity, 0.0, 1.0);
        Accuracy = Math.Max(0.0, Accuracy - severity);

        // Transition state based on remaining accuracy
        State = Accuracy switch
        {
            0.0    => SensorState.Dead,
            < 0.3  => SensorState.Fault,
            < 0.7  => SensorState.Noisy,
            _      => SensorState.OK
        };
    }

    /// <inheritdoc/>
    public void AddNoise(double amount)
    {
        _noiseBoost = Math.Min(1.0, _noiseBoost + amount);

        // Reflect noise in the displayed state even if accuracy is still high
        if (State == SensorState.OK && _noiseBoost > 0.1)
            State = SensorState.Noisy;
    }

    /// <inheritdoc/>
    public void ClearNoise()
    {
        _noiseBoost = 0;

        // Revert to the accuracy-based state (not Dead or Fault — those are permanent)
        if (State == SensorState.Noisy)
            State = Accuracy >= 0.7 ? SensorState.OK : SensorState.Noisy;
    }

    /// <inheritdoc/>
    public void Kill()
    {
        Accuracy    = 0;
        _noiseBoost = 0;
        State       = SensorState.Dead;
    }

    /// <inheritdoc/>
    public void Repair()
    {
        Accuracy    = 1.0;
        _noiseBoost = 0;
        State       = SensorState.OK;
    }
}