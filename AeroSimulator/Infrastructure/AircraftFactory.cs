using AeroSimulator.Core.Aircraft.Sensors;

namespace AeroSimulator.Infrastructure;

using AeroSimulator.Core.Aircraft;

// Wzorzec Factory (Fabryka), buduje skomplikowany obiekt modelu statku powietrznego na podstawie wybranej konfiguracji.
public static class AircraftFactory
{
    public static Aircraft Create(SimulationConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.Aircraft == null) throw new ArgumentNullException(nameof(config.Aircraft));
        
        var initialData = new FlightData(config.Aircraft.EngineCount)
        {
            Altitude = 0.0,
            Speed = 0.0,
            FuelLevelKg = config.Aircraft.MaxFuelKg,
            FuelCapacityKg = config.Aircraft.MaxFuelKg,
            TargetSpeed = config.Aircraft.CruiseSpeedKts,
            TargetAltitude = Math.Min(config.Aircraft.MaxAltitudeFt, 35000.0),
            Config = config.Aircraft
        };
        
        var sensorSystem = new SensorSystem(config.Aircraft.EngineCount);
        
        var aircraft = new Aircraft(config, initialData, sensorSystem);

        return aircraft;
    }
}