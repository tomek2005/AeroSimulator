using Xunit;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Systems;
using AeroSimulator.Core.Aircraft.Sensors;
using AeroSimulator.Core.States;
using AeroSimulator.Core.Strategies.Anomalies;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Events;
using AeroSimulator.Infrastructure; 

namespace AeroSimulator.Tests.Integration;

// Testy integracyjne, które sprawdzają kolejno czy:
// 1. Samolot z odpowiednią ilością paliwa poprawnie przechodzi ze stanu kołowania do stanu startu (TakeOffState).
// 2. Maszyna w trakcie startu automatycznie przełącza się w stan wznoszenia (ClimbState) po przekroczeniu 1500 stóp.
// 3. System podczas lotu (CruiseState) potrafi wykryć krytyczny brak paliwa (poniżej 5%) i wymusić stan awaryjny (EmergencyState).
// 4. Moduł anomalii poprawnie wyzwala awarię silnika w pętli symulacji i zgłasza swój status jako aktywny.


public class AircraftIntegrationTests
{
    private static Aircraft CreateAircraft(double fuelPercent = 100.0)
    {
        var aircraftCfg = new AircraftConfig
        {
            DisplayName        = "Test 737",
            TailNumber         = "SP-TST",
            MaxFuelKg          = 20_000,
            MaxAltitudeFt      = 41_000,
            CruiseSpeedKts     = 460,
            MaxSpeedKts        = 500,
            StallSpeedKts      = 130,
            StallSpeedFlaps    = 105,
            EngineCount        = 2,
            MaxThrustKN        = 120,
            MaxClimbRateFtMin  = 2500,
            NormalDescentFtMin = 1500,
            V1SpeedKts         = 150,
            VRSpeedKts         = 160,
            V2SpeedKts         = 170,
            MaxCrosswindKts    = 38,
            FuelBurnKgPerH     = 3_500,
            WingStrength       = 1.0
        };

        var simConfig = new SimulationConfig(Difficulty.Normal, aircraftCfg, null!);
        
        var flightData = new FlightData(2);
        var sensors = new SensorSystem(2);
        
        var aircraft = new Aircraft(simConfig, flightData, sensors);

        aircraft.FlightData.FuelLevelKg  = aircraftCfg.MaxFuelKg * (fuelPercent / 100.0);
        aircraft.FlightData.FuelCapacityKg = aircraftCfg.MaxFuelKg;

        return aircraft;
    }

    [Fact]
    public void GroundState_TakeOff_PrzechodziWTakeOffStateZPelnymBakiem()
    {
        var aircraft = CreateAircraft(fuelPercent: 100.0);
        aircraft.TransitionTo(new TaxiState()); 
        
        aircraft.TakeOff();

        Assert.IsType<TakeOffState>(aircraft.CurrentState);
    }

    [Fact]
    public void TakeOffState_Update_PrzechodziDoWznoszenia()
    {
        var aircraft = CreateAircraft(fuelPercent: 100.0);
        aircraft.TransitionTo(new TakeOffState());
        
        aircraft.FlightData.Altitude = 1600; 
        aircraft.Update(deltaT: 0.1);

        Assert.IsType<ClimbState>(aircraft.CurrentState);
    }

    [Fact]
    public void CruiseState_Update_WykrywaMaloPaliwa()
    {
        var aircraft = CreateAircraft(fuelPercent: 100.0);
        aircraft.TransitionTo(new CruiseState());
        
        aircraft.FlightData.FuelLevelKg = aircraft.FlightData.FuelCapacityKg * 0.04; 
        aircraft.Update(deltaT: 0.1);

        Assert.IsType<EmergencyState>(aircraft.CurrentState);
    }

    [Fact]
    public void EngineFailureAnomaly_Trigger_ZglaszaUsterkeDoSystemu()
    {
        var aircraft = CreateAircraft(fuelPercent: 100.0);
        aircraft.TransitionTo(new CruiseState());
        var anomaly = new EngineFailureAnomaly();

        anomaly.Trigger(aircraft, aircraft.FlightData);

        for (int i = 0; i < 10; i++)
        {
             aircraft.Update(1.0);
        }

        Assert.True(anomaly.IsActive, "Anomalia powinna zgłosić swój status jako aktywna");
    }
}