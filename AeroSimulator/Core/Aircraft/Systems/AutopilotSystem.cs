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

        // --- 1. BEZPIECZNE ODCZYTYWANIE MONAD (OPTION) ---
        var altReading = sensors.GetReading(sensors.Altitude.SensorName);
        var spdReading = sensors.GetReading(sensors.Airspeed.SensorName);

        // --- 2. DELEGOWANIE DO CZYSTYCH FUNKCJI ---
        // Wykonujemy zmianę tylko wtedy, gdy czujnik żyje (HasValue == true)
        if (altReading.HasValue)
        {
            data.PitchAngleDeg = CalculateNewPitch(TargetAltitude, altReading.Value, data.PitchAngleDeg, deltaT);
        }

        // Heading nie korzysta z czujników zewnętrznych, tylko z bezpiecznego IMU (FlightData)
        data.RollAngleDeg = CalculateNewRoll(TargetHeading, data.Heading, data.RollAngleDeg, deltaT);

        if (TargetSpeed > 0 && spdReading.HasValue)
        {
            data.Throttle = CalculateNewThrottle(TargetSpeed, spdReading.Value, data.Throttle, deltaT);
        }
    }

    // =========================================================================
    // PARADYGMAT FUNKCYJNY: CZYSTE FUNKCJE (PURE FUNCTIONS)
    // - Metody statyczne
    // - Zależą TYLKO od argumentów wejściowych
    // - Nie modyfikują stanu obiektu (brak skutków ubocznych)
    // - Idealne do wyizolowanego testowania jednostkowego (Unit Tests)
    // =========================================================================

    public static double CalculateNewPitch(double targetAlt, double currentAlt, double currentPitch, double deltaT)
    {
        double altitudeError = targetAlt - currentAlt;
        return Math.Clamp(currentPitch + altitudeError * 0.0006 * deltaT, -8.0, 8.0);
    }

    public static double CalculateNewRoll(double targetHeading, double currentHeading, double currentRoll, double deltaT)
    {
        double headingError = NormalizeHeadingError(targetHeading - currentHeading);
        return Math.Clamp(currentRoll + headingError * 0.05 * deltaT, -25.0, 25.0);
    }

    public static double CalculateNewThrottle(double targetSpeed, double currentSpeed, double currentThrottle, double deltaT)
    {
        double speedError = targetSpeed - currentSpeed;
        return Math.Clamp(currentThrottle + speedError * 0.002 * deltaT, 0.0, 1.0);
    }

    private static double NormalizeHeadingError(double error)
    {
        while (error > 180.0) error -= 360.0;
        while (error < -180.0) error += 360.0;
        return error;
    }
}