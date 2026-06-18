using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Common; 

namespace AeroSimulator.Core.Aircraft.Sensors;

// Common contract for every sensor on the aircraft.
// they may add noise, stick on stale values, or return Option.None() when dead.
// The view and autopilot always read from sensors, never from FlightData directly.
public interface ISensor
{
    string SensorName { get; }
    SensorState State { get; }
    double Accuracy { get; }
    Option<double> Read(double realValue);
    void ApplyDamage(double severity);
    void AddNoise(double amount);
    void ClearNoise();
    void Kill();
    void Repair();
}