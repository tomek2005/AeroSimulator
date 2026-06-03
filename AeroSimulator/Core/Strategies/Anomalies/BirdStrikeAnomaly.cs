using System;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public sealed class BirdStrikeAnomaly : AbstractAnomaly
{
    private const double EngineDamage            = 0.30;
    private const double GForceSpike             = 0.50;
    private const double CascadeFireChance       = 0.40;
    private const double VibrationGForce         = 0.15;
    private const double SensorDamageThreshold   = 0.50;
    private const double SensorDamageAmount      = 0.40;
    private const double AutoResolveSec          = 10.0;

    private int  _struckEngineIndex;
    private bool _sensorDamageApplied;

    public override string   AnomalyName   => "BIRD STRIKE";
    public override string   Description   => "Foreign object ingested by engine — structural damage imminent.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0008;
    public override bool     CanBeResolved => false;

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_struckEngineIndex + 1} BIRD STRIKE -- check RPM sensor !!";

    public override string GetPilotAction() =>
        "Monitor engine RPM and temperature. Prepare for engine fire procedure.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        if (data.Altitude > 10_000) { SelfResolve(); return; }

        // Dynamiczne losowanie silnika na podstawie konfiguracji
        _struckEngineIndex   = _rng.Next(0, ctx.EngineCount); 
        _sensorDamageApplied = false;

        // Bezpośrednie uszkodzenie fizycznego silnika
        ctx.GetEngine(_struckEngineIndex).Health -= EngineDamage;

        data.GForce += GForceSpike;

        if (RollChance(CascadeFireChance))
            TriggerCascade(ctx, new EngineFireAnomaly(_struckEngineIndex));
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        double vibration = (_rng.NextDouble() - 0.5) * 2.0 * VibrationGForce;
        data.GForce = Math.Max(0.8, data.GForce + vibration);

        // Pobranie zdrowia uszkodzonego silnika
        double engineHealth = ctx.GetEngine(_struckEngineIndex).Health;

        if (!_sensorDamageApplied && engineHealth < SensorDamageThreshold)
        {
            _sensorDamageApplied = true;
            // Pobranie czujnika z dynamicznej tablicy
            var sensor = ctx.Sensors.EngineRPM[_struckEngineIndex];
            sensor.ApplyDamage(SensorDamageAmount);
        }

        if (_activeDuration >= AutoResolveSec)
            SelfResolve();
    }

    protected override bool OnResolve(Aircraft ctx) => false;
}