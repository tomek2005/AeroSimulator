using System;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Aircraft;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public sealed class TurbulenceAnomaly : AbstractAnomaly
{
    private const double AltOscillationFt = 200;
    private const double SpeedOscillationKts = 15;
    private const double GForceOscillation = 0.30;
    private const double SensorNoiseAmount = 0.15;
    private const double MinDurationSec = 180;
    private const double MaxDurationSec = 480;

    private Severity _turbulenceSeverity;
    private double _totalDuration;
    private bool _sensorNoiseApplied;
    private bool _criticalSensorDamaged;

    public override string AnomalyName => "TURBULENCE";
    public override string Description => "Severe atmospheric turbulence — sensor readings may be unreliable.";
    public override Severity Level => _turbulenceSeverity;
    public override double Probability => 0.0015;
    public override bool CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: {_turbulenceSeverity.ToString().ToUpper()} TURBULENCE -- sensor noise active !!";

    public override string GetPilotAction() =>
        "Ride it out. Maintain altitude and speed manually. Wait for turbulence to subside.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _turbulenceSeverity = (Severity)_rng.Next(1, 5);
        _totalDuration = MinDurationSec + _rng.NextDouble() * (MaxDurationSec - MinDurationSec);
        _sensorNoiseApplied = false;
        _criticalSensorDamaged = false;

        if (_turbulenceSeverity >= Severity.Medium)
        {
            _sensorNoiseApplied = true;
            ctx.Sensors.AddNoiseToAll(SensorNoiseAmount);
        }

        if (_turbulenceSeverity == Severity.Critical && !_criticalSensorDamaged)
        {
            _criticalSensorDamaged = true;
            ctx.Sensors.DamageRandomSensor();
        }
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        data.Altitude += (_rng.NextDouble() - 0.5) * 2 * AltOscillationFt * deltaT;
        data.Speed += (_rng.NextDouble() - 0.5) * 2 * SpeedOscillationKts * deltaT;
        data.GForce += (_rng.NextDouble() - 0.5) * 2 * GForceOscillation * deltaT;
        data.GForce = Math.Clamp(data.GForce, 0.2, 4.0);
        data.PitchAngleDeg += (_rng.NextDouble() - 0.5) * 4 * deltaT;
        data.RollAngleDeg += (_rng.NextDouble() - 0.5) * 6 * deltaT;

        if (_activeDuration >= _totalDuration)
        {
            SelfResolve();
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        if (_sensorNoiseApplied)
        {
            ctx.Sensors.ClearNoiseFromAll();
            _sensorNoiseApplied = false;
        }

        return true;
    }
}