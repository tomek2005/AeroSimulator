namespace AeroSimulator.Core.Aircraft.Systems;

using AeroSimulator.Core.Aircraft.Sensors;

public class AutopilotSystem : IAircraftSystem
{
    public bool IsEngaged { get; private set; }
    public double TargetAltitude { get; private set; }
    public double TargetHeading { get; private set; }
    public double TargetSpeed { get; private set; }
    public bool IsOffline { get; private set; }

    public void Engage() 
    {
        if (!IsOffline) IsEngaged = true;
    }

    public void Engage(double altitude, double heading, double speed)
    {
        if (IsOffline) return;

        TargetAltitude = altitude;
        TargetHeading = heading;
        TargetSpeed = speed;
        IsEngaged = true;
    }
    
    public void Disengage() => IsEngaged = false;

    public void SetTargetAltitude(double altitude)
    {
        if (!IsOffline) TargetAltitude = altitude;
    }

    public void ResyncAltitude(double altitude)
    {
        if (IsEngaged) TargetAltitude = altitude;
    }

    public void SetOffline()
    {
        IsOffline = true;
        Disengage();
    }

    public bool Reboot()
    {
        IsOffline = false;
        return true;
    }

    public void Update(FlightData data, SensorSystem sensors, double deltaT)
    {
        if (!IsEngaged || IsOffline) return;

        double sensedAltitude = sensors.GetReading(sensors.Altitude.SensorName);
        double sensedSpeed = sensors.GetReading(sensors.Airspeed.SensorName);

        if (sensedAltitude >= 0)
        {
            double altitudeError = TargetAltitude - sensedAltitude;
            data.PitchAngleDeg = Math.Clamp(data.PitchAngleDeg + altitudeError * 0.0006 * deltaT, -8.0, 8.0);
        }

        double headingError = NormalizeHeadingError(TargetHeading - data.Heading);
        data.RollAngleDeg = Math.Clamp(data.RollAngleDeg + headingError * 0.05 * deltaT, -25.0, 25.0);

        if (TargetSpeed > 0 && sensedSpeed >= 0)
        {
            double speedError = TargetSpeed - sensedSpeed;
            data.Throttle = Math.Clamp(data.Throttle + speedError * 0.002 * deltaT, 0.0, 1.0);
        }
    }

    private static double NormalizeHeadingError(double error)
    {
        while (error > 180.0) error -= 360.0;
        while (error < -180.0) error += 360.0;
        return error;
    }
}
