using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

// Airframe and pitot icing anomaly. Only valid below 0 °C.
// Ice raises the effective stall speed by 1 kt/min and adds progressive
// noise to the airspeed sensor. Resolved by activating de-icing.
public sealed class IcingAnomaly : AbstractAnomaly
{
    private const double TempThresholdC = 0.0;
    private const double StallSpeedIncreasePerSec = 1.0 / 60.0;
    private const double EscalationThresholdKts = 15.0;
    private const double PitotNoisePerSec = 0.003;
    private const double MaxPitotNoise = 0.40;

    private double _stallSpeedIncrease;
    private double _currentPitotNoise;
    private bool _escalated;

    public override string AnomalyName => "ICING";
    public override string Description => "Ice accumulation on airframe — stall speed rising, airspeed unreliable.";
    public override Severity Level => _escalated ? Severity.High : Severity.Medium;
    public override double Probability => 0.0008;
    public override bool CanBeResolved => true;

    public override string GetWarningMessage() =>
        $"!! WARNING: ICING -- stall speed +{_stallSpeedIncrease:F1} kts, pitot sensor noisy !!";

    public override string GetPilotAction() =>
        "Press [R] to activate de-icing system. Reduce speed cautiously.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        if (data.TemperatureC >= TempThresholdC)
        {
            SelfResolve();
            return;
        }

        _stallSpeedIncrease = 0;
        _currentPitotNoise = 0;
        _escalated = false;
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (data.TemperatureC >= TempThresholdC) return;

        _stallSpeedIncrease += StallSpeedIncreasePerSec * deltaT;
        
        data.UpdateStallSpeedOffset(_stallSpeedIncrease);

        double newNoise = Math.Min(PitotNoisePerSec * deltaT, MaxPitotNoise - _currentPitotNoise);
        if (newNoise > 0)
        {
            _currentPitotNoise += newNoise;
            ctx.Sensors.Airspeed.AddNoise(newNoise);
        }

        if (!_escalated && _stallSpeedIncrease >= EscalationThresholdKts)
        {
            _escalated = true;
            PublishAlert(ctx,
                $"ICING SEVERE -- stall speed +{_stallSpeedIncrease:F0} kts, ACTIVATE DE-ICING",
                Severity.High);
        }
    }

    protected override bool OnResolve(Aircraft ctx)
    {
        bool deIced = ctx.ElectricalSystem.ActivateDeIcing();
        if (deIced)
        {
            ctx.FlightData.UpdateStallSpeedOffset(0);
            ctx.Sensors.Airspeed.ClearNoise();
            _stallSpeedIncrease = 0;
            _currentPitotNoise = 0;
        }

        return deIced;
    }
}