using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Fuel leak anomaly. Starts a leak at 80–250 kg/h. The fuel sensor reads
/// slightly higher than reality (fuel slosh confuses the sensor). Every 60
/// seconds unresolved, a 20 % ignition risk check fires — if it triggers,
/// the leak cascades into an <see cref="EngineFireAnomaly"/>.
/// </summary>
public sealed class FuelLeakAnomaly : AbstractAnomaly
{
    private const double MinLeakRateKgH       = 80.0;
    private const double MaxLeakRateKgH       = 250.0;
    private const double FuelSensorNoiseBoost = 0.12;
    private const double IgnitionCheckSec     = 60.0;
    private const double IgnitionChance       = 0.20;

    private double _leakRateKgH;
    private double _timeSinceIgnitionCheck;

    public override string   AnomalyName   => "FUEL LEAK";
    public override string   Description   => "Fuel system breach detected — loss of fuel and fire risk.";
    public override Severity Level         => Severity.High;
    public override double   Probability   => 0.0005;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: FUEL LEAK detected -- {_leakRateKgH:F0} kg/h loss rate !!";

    public override string GetPilotAction() =>
        "Press [R] to seal fuel leak. Monitor fuel level closely.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        _leakRateKgH            = MinLeakRateKgH + _rng.NextDouble() * (MaxLeakRateKgH - MinLeakRateKgH);
        _timeSinceIgnitionCheck = 0;

        ctx.FuelSystem.StartLeak(_leakRateKgH);
        ctx.Sensors.FuelLevel.AddNoise(FuelSensorNoiseBoost);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        _timeSinceIgnitionCheck += deltaT;

        if (_timeSinceIgnitionCheck >= IgnitionCheckSec)
        {
            _timeSinceIgnitionCheck = 0;

            if (ctx.FuelSystem.CheckIgnitionRisk() && RollChance(IgnitionChance))
            {
                int engineIdx = _rng.Next(0, ctx.EngineCount);
                TriggerCascade(ctx, new EngineFireAnomaly(engineIdx));
            }
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        bool sealed_ = ctx.FuelSystem.SealLeak();
        if (sealed_)
            ctx.Sensors.FuelLevel.ClearNoise();
        return sealed_;
    }
}