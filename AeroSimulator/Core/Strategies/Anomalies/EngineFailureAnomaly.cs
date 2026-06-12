using System;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public sealed class EngineFailureAnomaly : AbstractAnomaly
{
    private const double SingleEngineSpeedDecayPerSec = 0.5;
    private const double RestartMinAltitudeFt         = 5_000;

    private readonly int _engineIndex;

    public EngineFailureAnomaly(int engineIndex = 0)
    {
        _engineIndex = engineIndex;
    }

    public override string   AnomalyName   => $"ENGINE {_engineIndex + 1} FAILURE";
    public override string   Description   => $"Engine {_engineIndex + 1} flame-out — thrust lost.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0003;
    public override bool     CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! ALERT: ENGINE {_engineIndex + 1} FLAME-OUT -- RPM decaying to zero !!";

    public override string GetPilotAction() =>
        "Press [R] to attempt in-flight engine restart. Maintain speed above stall.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        ctx.GetEngine(_engineIndex).Stop();

        // Dynamiczny czujnik
        var rpmSensor = ctx.Sensors.EngineRPMs[_engineIndex];
        rpmSensor.ApplyDamage(0.7);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        var otherEngine = ctx.GetEngine(_engineIndex == 0 ? 1 : 0);
        if (otherEngine.Health <= 0.1)
            data.Speed = Math.Max(0, data.Speed - SingleEngineSpeedDecayPerSec * deltaT);
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        if (ctx.FlightData.Altitude < RestartMinAltitudeFt)
            return false;

        return ctx.GetEngine(_engineIndex).Restart();
    }
}