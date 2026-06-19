namespace AeroSimulator.Core.Aircraft;

/// Mutable record of all live flight telemetry values. This is the ground truth — systems write here, sensors read
public class FlightData
{
    public double Altitude { get; set; }
    public double Speed { get; set; }
    public double VerticalSpeed { get; set; }
    public double Heading { get; set; }
    public double PitchAngleDeg { get; set; }
    public double RollAngleDeg { get; set; }
    public double GForce { get; set; } = 1.0;

    public double TargetHeading { get; set; }
    public double TargetAltitude { get; set; }
    public double TargetSpeed { get; set; }

    public double MapX { get; set; }
    public double MapY { get; set; }

    public double DistanceToDestinationNm { get; set; }
    public double AirportElevation { get; set; } = 0;
    public string DestinationName { get; set; } = "EPWA (Warsaw Chopin)";

    public double Throttle { get; set; }
    public double[] EngineRPMs { get; set; }
    public double[] EngineTempsC { get; set; }

    public double FuelLevelKg { get; set; }
    public double FuelFlowKgPerH { get; set; }
    public double FuelCapacityKg { get; set; }

    public double WindSpeedKnots { get; set; }
    public double WindDirectionDeg { get; set; }
    public double AirPressureHPa { get; set; } = 1013.25;
    public double TemperatureC { get; set; }
    public double StallSpeedOffset { get; set; }

    public TimeSpan FlightTime { get; set; } = TimeSpan.Zero;

    public double AsymmetricDrag { get; set; }

    public AircraftConfig? Config { get; set; }
    
    public FlightData(int engineCount)
    {
        if (engineCount < 1)
            throw new ArgumentException("Must have at least 1 engine.", nameof(engineCount));

        EngineRPMs = new double[engineCount];
        EngineTempsC = new double[engineCount];
        Reset(); 
    }

    public double FuelRemainingPercent() =>
        FuelCapacityKg > 0 ? FuelLevelKg / FuelCapacityKg * 100.0 : 0.0;

    public double EstimatedRangeKm()
    {
        if (FuelFlowKgPerH <= 0) return 0;
        double hoursRemaining = FuelLevelKg / FuelFlowKgPerH;
        return hoursRemaining * Speed * 1.852;
    }

    public bool IsStalling()
    {
        if (Config is null) return false;
        return Speed < Config.StallSpeedFlaps && Altitude > 0;
    }

    public bool IsOverspeed()
    {
        if (Config is null) return false;
        return Speed > Config.MaxSpeedKts;
    }

    public bool IsInLandingZone()
    {
        bool isClose = DistanceToDestinationNm <= 5.0;
        bool isLow = (Altitude - AirportElevation) < 1500 && (Altitude - AirportElevation) > 0;
        bool isSlowEnough = Speed < 160; 

        return isClose && isLow && isSlowEnough;
    }

    public void UpdateNavigation(double deltaTimeSeconds)
    {
        if (DistanceToDestinationNm > 0)
        {
            double distanceCovered = (Speed / 3600.0) * deltaTimeSeconds;
            DistanceToDestinationNm = Math.Max(0, DistanceToDestinationNm - distanceCovered);
        }
    }
    
    public void ApplyGForceSpike(double spike)
    {
        GForce += spike;
    }
    
    public void ApplyVibration(double vibrationAmount)
    {
        GForce = Math.Max(0.5, GForce + vibrationAmount);
    }
    
    public void ApplyWindVector(double speedKnots, double directionDeg)
    {
        WindSpeedKnots = speedKnots;
        WindDirectionDeg = directionDeg % 360.0;
    }
    
    public void ResetWind()
    {
        WindSpeedKnots = 0;
    }
    
    public void UpdateStallSpeedOffset(double offsetAmount)
    {
        StallSpeedOffset = offsetAmount;
    }

    public FlightDataSnapshot Snapshot() => new(this);
    
    public string ToTelemetryString()
    {
        string engineRpmsCsv = string.Join(",", EngineRPMs.Select(rpm => rpm.ToString("F1")));

        return string.Join(",",
            DateTime.UtcNow.ToString("HH:mm:ss"),
            Altitude.ToString("F0"),
            Speed.ToString("F1"),
            VerticalSpeed.ToString("F0"),
            Heading.ToString("F1"),
            Throttle.ToString("F2"),
            engineRpmsCsv,
            FuelLevelKg.ToString("F0"),
            GForce.ToString("F2"),
            PitchAngleDeg.ToString("F1"),
            RollAngleDeg.ToString("F1"),
            DistanceToDestinationNm.ToString("F1"));
    }

    public void Reset()
    {
        Altitude = 0;
        Speed = 0;
        VerticalSpeed = 0;
        Heading = 360;
        PitchAngleDeg = 0;
        RollAngleDeg = 0;
        GForce = 1.0;
        Throttle = 0;
        FuelFlowKgPerH = 0;
        FlightTime = TimeSpan.Zero;
        AsymmetricDrag = 0;
        MapX = 0;
        MapY = 0;
        DistanceToDestinationNm = 100.0;
        AirportElevation = 0;
        
        for (int i = 0; i < EngineRPMs.Length; i++)
        {
            EngineRPMs[i] = 0;
            EngineTempsC[i] = 15;
        }
    }

    public void ApplyAsymmetricDrift(double driftDegPerSec, double dt)
    {
        Heading = (Heading + driftDegPerSec * dt + 360.0) % 360.0;
    }
}

public record FlightDataSnapshot
{
    public DateTime Timestamp { get; init; }

    public double Altitude { get; init; }
    public double Speed { get; init; }
    public double VerticalSpeed { get; init; }
    public double Heading { get; init; }
    public double PitchAngleDeg { get; init; }
    public double RollAngleDeg { get; init; }
    public double GForce { get; init; }
    public double Throttle { get; init; }

    public double[] EngineRPMs { get; init; }
    public double[] EngineTempsC { get; init; }
    public double FuelLevelKg { get; init; }
    public double FuelFlowKgPerH { get; init; }
    public double WindSpeedKnots { get; init; }
    public double AsymmetricDrag { get; init; }
    public TimeSpan FlightTime { get; init; }
    public double DistanceToDestinationNm { get; init; }

    public FlightDataSnapshot(FlightData src)
    {
        Timestamp = DateTime.UtcNow;
        Altitude = src.Altitude;
        Speed = src.Speed;
        VerticalSpeed = src.VerticalSpeed;
        Heading = src.Heading;
        PitchAngleDeg = src.PitchAngleDeg;
        RollAngleDeg = src.RollAngleDeg;
        GForce = src.GForce;
        Throttle = src.Throttle;
        
        EngineRPMs = src.EngineRPMs.ToArray();
        EngineTempsC = src.EngineTempsC.ToArray();

        FuelLevelKg = src.FuelLevelKg;
        FuelFlowKgPerH = src.FuelFlowKgPerH;
        WindSpeedKnots = src.WindSpeedKnots;
        AsymmetricDrag = src.AsymmetricDrag;
        FlightTime = src.FlightTime;
        DistanceToDestinationNm = src.DistanceToDestinationNm;
    }

    public string ToCsvRow()
    {
        string engineRpmsCsv = string.Join(",", EngineRPMs.Select(rpm => rpm.ToString("F1")));

        return string.Join(",",
            Timestamp.ToString("HH:mm:ss"),
            Altitude.ToString("F0"),
            Speed.ToString("F1"),
            VerticalSpeed.ToString("F0"),
            Heading.ToString("F1"),
            Throttle.ToString("F2"),
            engineRpmsCsv,
            FuelLevelKg.ToString("F0"),
            GForce.ToString("F2"),
            PitchAngleDeg.ToString("F1"),
            RollAngleDeg.ToString("F1"),
            DistanceToDestinationNm.ToString("F1"));
    }
}