using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Common; // <-- DODANY IMPORT NASZEJ MONADY

namespace AeroSimulator.Core.Aircraft.Sensors;

/// <summary>
/// Common contract for every sensor on the aircraft.
/// Sensors sit between raw <see cref="FlightData"/> values and the view:
/// they may add noise, stick on stale values, or return Option.None() when dead.
/// The view and autopilot always read from sensors, never from FlightData directly.
/// </summary>
public interface ISensor
{
    /// <summary>Short identifier shown on the dashboard, e.g. "ALT-SNS", "ENG1-RPM".</summary>
    string SensorName { get; }

    /// <summary>Current operational state of this sensor.</summary>
    SensorState State { get; }

    /// <summary>
    /// Signal accuracy from 1.0 (perfect) to 0.0 (dead).
    /// Decreases as damage accumulates; drives noise magnitude.
    /// </summary>
    double Accuracy { get; }

    /// <summary>
    /// Returns a reading of <paramref name="realValue"/>, possibly distorted by
    /// noise, stuck at a stale value (Fault), or None (Dead).
    /// </summary>
    /// <param name="realValue">The true physical quantity from <see cref="FlightData"/>.</param>
    Option<double> Read(double realValue); // <-- KLUCZOWA ZMIANA Z DOUBLE NA OPTION<DOUBLE>

    /// <summary>
    /// Reduces sensor accuracy and may transition it to Noisy, Fault, or Dead.
    /// </summary>
    /// <param name="severity">Damage amount (0.0–1.0).</param>
    void ApplyDamage(double severity);

    /// <summary>
    /// Adds temporary noise on top of base inaccuracy (e.g. turbulence effect).
    /// Accumulates with multiple calls; clear with <see cref="ClearNoise"/>.
    /// </summary>
    /// <param name="amount">Extra noise factor (0.0–1.0).</param>
    void AddNoise(double amount);

    /// <summary>Removes all temporary noise boost while leaving accuracy unchanged.</summary>
    void ClearNoise();

    /// <summary>Forces the sensor into the Dead state; always returns None thereafter.</summary>
    void Kill();

    /// <summary>Resets the sensor to full accuracy and OK state.</summary>
    void Repair();
}