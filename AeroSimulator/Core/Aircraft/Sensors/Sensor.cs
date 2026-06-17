using System;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Common; 

namespace AeroSimulator.Core.Aircraft.Sensors;

// Base sensor implementation with Gaussian-style noise, fault sticking, and dead-return behaviour using the Option monad.
// Subclasses override Scale to apply unit-specific noise scaling.
public class Sensor : ISensor
{
    private readonly Random _rng = new();
    private double _noiseBoost;
    private double _lastReading;
    public string SensorName { get; }
    public SensorState State { get; private set; } = SensorState.OK;
    public double Accuracy { get; private set; } = 1.0;
    
    protected virtual double Scale => 0.15;
    
    public Sensor(string sensorName)
    {
        SensorName = sensorName;
        _lastReading = 0;
    }
    
    public Option<double> Read(double realValue)
    {
        switch (State)
        {
            case SensorState.Dead:
                return Option<double>.None();

            case SensorState.Fault:
                return Option<double>.Some(_lastReading);

            default:
                double totalNoiseLevel = (1.0 - Accuracy) + _noiseBoost;
                double noiseMagnitude = realValue * Scale * totalNoiseLevel;
                double noiseSample = (_rng.NextDouble() - 0.5) * 2.0 * noiseMagnitude;
                _lastReading = realValue + noiseSample;
                
                return Option<double>.Some(_lastReading);
        }
    }
    
    public void ApplyDamage(double severity)
    {
        severity = Math.Clamp(severity, 0.0, 1.0);
        Accuracy = Math.Max(0.0, Accuracy - severity);
        
        State = Accuracy switch
        {
            0.0 => SensorState.Dead,
            < 0.3 => SensorState.Fault,
            < 0.7 => SensorState.Noisy,
            _ => SensorState.OK
        };
    }
    
    public void AddNoise(double amount)
    {
        _noiseBoost = Math.Min(1.0, _noiseBoost + amount);
        
        if (State == SensorState.OK && _noiseBoost > 0.1)
            State = SensorState.Noisy;
    }
    
    public void ClearNoise()
    {
        _noiseBoost = 0;

        if (State == SensorState.Noisy)
            State = Accuracy >= 0.7 ? SensorState.OK : SensorState.Noisy;
    }
    
    public void Kill()
    {
        Accuracy = 0;
        _noiseBoost = 0;
        State = SensorState.Dead;
    }
    
    public void Repair()
    {
        Accuracy = 1.0;
        _noiseBoost = 0;
        State = SensorState.OK;
    }
}