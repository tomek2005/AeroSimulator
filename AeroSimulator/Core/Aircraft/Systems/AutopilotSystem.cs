namespace AeroSimulator.Core.Aircraft.Systems;

using System;
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

        // Aktualizacja stanu następuje poprzez przypisanie wyniku z Czystych Funkcji.
        // Sama metoda Update staje się tylko "łącznikiem" między modelem a kalkulatorem.

        data.PitchAngleDeg = AutopilotCalculator.CalculateNewPitch(
            data.PitchAngleDeg, TargetAltitude, sensedAltitude, deltaT);

        data.RollAngleDeg = AutopilotCalculator.CalculateNewRoll(
            data.RollAngleDeg, TargetHeading, data.Heading, deltaT);

        data.Throttle = AutopilotCalculator.CalculateNewThrottle(
            data.Throttle, TargetSpeed, sensedSpeed, deltaT);
    }
}

/// Moduł funkcyjny (Functional Programming) zawierający wyłącznie Czyste Funkcje (Pure Functions).
/// Brak stanu wewnętrznego, brak ukrytych mutacji, całkowicie deterministyczne wyniki.
public static class AutopilotCalculator
{
    public static double CalculateNewPitch(double currentPitch, double targetAltitude, double sensedAltitude, double deltaT)
    {
        if (sensedAltitude < 0) return currentPitch;
        
        double altitudeError = targetAltitude - sensedAltitude;
        return Math.Clamp(currentPitch + altitudeError * 0.0006 * deltaT, -8.0, 8.0);
    }

    public static double CalculateNewRoll(double currentRoll, double targetHeading, double currentHeading, double deltaT)
    {
        double headingError = NormalizeHeadingError(targetHeading - currentHeading);
        return Math.Clamp(currentRoll + headingError * 0.05 * deltaT, -25.0, 25.0);
    }

    public static double CalculateNewThrottle(double currentThrottle, double targetSpeed, double sensedSpeed, double deltaT)
    {
        if (targetSpeed <= 0 || sensedSpeed < 0) return currentThrottle;

        double speedError = targetSpeed - sensedSpeed;
        return Math.Clamp(currentThrottle + speedError * 0.002 * deltaT, 0.0, 1.0);
    }

    private static double NormalizeHeadingError(double error)
    {
        while (error > 180.0) error -= 360.0;
        while (error < -180.0) error += 360.0;
        return error;
    }
}