using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

/// <summary>
/// Rapid explosive decompression. Only valid above 25 000 ft. Forces an
/// immediate emergency descent to 10 000 ft. Altitude and airspeed sensors
/// get heavy noise. Failure to descend within 60 seconds → pilot incapacitation
/// → GAME OVER.
/// </summary>
public sealed class DecompressionAnomaly : AbstractAnomaly
{
    private const double MinAltitudeFt         = 25_000;
    private const double SafeAltitudeFt        = 10_000;
    private const double PressureNoiseBoost    = 0.30;
    private const double IncapacitationTimeSec = 60.0;

    private bool _incapacitated;

    public override string   AnomalyName   => "DECOMPRESSION";
    public override string   Description   => "Explosive decompression — emergency descent to 10 000 ft required.";
    public override Severity Level         => Severity.Critical;
    public override double   Probability   => 0.0003;
    public override bool     CanBeResolved => false;

    public override string GetWarningMessage() =>
        "!! MAYDAY: DECOMPRESSION -- DESCEND IMMEDIATELY TO 10 000 FT !!";

    public override string GetPilotAction() =>
        "Press [X] repeatedly to descend below 10 000 ft. 60 seconds before incapacitation.";

    protected override void OnTrigger(Aircraft ctx, FlightData data)
    {
        if (data.Altitude < MinAltitudeFt) { SelfResolve(); return; }

        _incapacitated = false;

        data.TargetAltitude = SafeAltitudeFt;
        ctx.AutopilotSystem.SetTargetAltitude(SafeAltitudeFt);

        ctx.Sensors.Altitude.AddNoise(PressureNoiseBoost);
        ctx.Sensors.Airspeed.AddNoise(PressureNoiseBoost);
    }

    protected override void OnUpdate(Aircraft ctx, FlightData data, double deltaT)
    {
        if (_incapacitated) return;

        // Player descended to safe altitude → resolve
        if (data.Altitude < SafeAltitudeFt)
        {
            ctx.Sensors.Altitude.ClearNoise();
            ctx.Sensors.Airspeed.ClearNoise();
            SelfResolve();
            return;
        }

        // 60-second countdown
        if (_activeDuration >= IncapacitationTimeSec)
        {
            _incapacitated                 = true;
            ctx.DamageModel.IsGameOver     = true;
            ctx.DamageModel.GameOverReason = "Pilot incapacitation — failed to descend after decompression";
        }
    }

    protected override bool OnResolve(Aircraft ctx) => false;
}