using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

/// <summary>
/// Structural wing fire cascade. Disables wing control systems and introduces
/// aerodynamic asymmetric drag, requiring pilot correction.
/// </summary>
public sealed class WingFireAnomaly : AbstractAnomaly
{
    private const double DragIncreasePerSec = 0.05;
    private const double MaxAsymmetricDrag  = 0.80;

    public override string   AnomalyName   => "WING STRUCTURAL FIRE";
    public override string   Description   => "Fire spread to wing structure — flight controls compromised, high drag.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0; // Wywoływane wyłącznie jako kaskada
    public override bool     CanBeResolved => false; // Uszkodzenie strukturalne skrzydeł jest nieodwracalne w locie

    public override string GetWarningMessage() =>
        "!! CRITICAL ALERT: WING STRUCTURAL FIRE -- flight controls frozen !!";

    public override string GetPilotAction() =>
        "Counteract asymmetric drift manually. Plan immediate emergency landing.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        // Odcięcie systemów sterowania skrzydłami (klapy, spoilery zamarzają)
        ctx.WingSystem.SetOffline();
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        // Pożar niszczy aerodynamikę skrzydła, generując narastający ciąg asymetryczny
        if (data.AsymmetricDrag < MaxAsymmetricDrag)
        {
            data.AsymmetricDrag = System.Math.Min(MaxAsymmetricDrag, data.AsymmetricDrag + DragIncreasePerSec * deltaT);
        }

        // Symulacja uciekania samolotu w bok pod wpływem oporu uszkodzonego skrzydła
        data.ApplyAsymmetricDrift(15.0 * data.AsymmetricDrag, deltaT);
    }

    protected override bool OnResolve(Aircraft ctx) => false;
}