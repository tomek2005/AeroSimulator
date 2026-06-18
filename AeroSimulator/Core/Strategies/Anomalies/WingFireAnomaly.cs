using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

/// Structural wing fire cascade. Disables wing control systems and introduces
/// aerodynamic asymmetric drag, requiring pilot correction.
public sealed class WingFireAnomaly : AbstractAnomaly
{
    private const double DragIncreasePerSec = 0.05;
    private const double MaxAsymmetricDrag = 0.80;

    public override string AnomalyName => "WING STRUCTURAL FIRE";
    public override string Description => "Fire spread to wing structure — flight controls compromised, high drag.";
    public override Severity Level => Severity.Critical;
    public override double Probability => 0.0;
    public override bool CanBeResolved => false;

    public override string GetWarningMessage() =>
        "!! CRITICAL ALERT: WING STRUCTURAL FIRE -- flight controls frozen !!";

    public override string GetPilotAction() =>
        "Counteract asymmetric drift manually. Plan immediate emergency landing.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        ctx.WingSystem.SetOffline();
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (data.AsymmetricDrag < MaxAsymmetricDrag)
        {
            data.AsymmetricDrag = System.Math.Min(MaxAsymmetricDrag, data.AsymmetricDrag + DragIncreasePerSec * deltaT);
        }
        
        data.ApplyAsymmetricDrift(15.0 * data.AsymmetricDrag, deltaT);
    }

    protected override bool OnResolve(Aircraft ctx) => false;
}