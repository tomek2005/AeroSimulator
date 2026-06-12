using AeroSimulator.Core.Aircraft.Sensors;

namespace AeroSimulator.Infrastructure;

using AeroSimulator.Core.Aircraft;

/// <summary>
/// Wzorzec Factory (Fabryka). Zgodnie z założeniami z README, buduje 
/// skomplikowany obiekt modelu statku powietrznego na podstawie wybranej konfiguracji.
/// </summary>
public static class AircraftFactory
{
    public static Aircraft Create(SimulationConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.Aircraft == null) throw new ArgumentNullException(nameof(config.Aircraft));

        // POPRAWKA: Przekazujemy liczbę silników do nowego konstruktora FlightData!
        // Dzięki temu klasa sama inicjalizuje tablice EngineRPMs i EngineTempsC
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

        // Budowa systemu czujników
        var sensorSystem = new SensorSystem(config.Aircraft.EngineCount);

        // Powołanie do życia głównego Modelu
        var aircraft = new Aircraft(config, initialData, sensorSystem);

        return aircraft;
    }
}
