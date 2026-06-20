using FluentAssertions;

using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Sensors;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Strategies.Anomalies;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Events.Handlers;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Tests.Core.Simulation;

public class AnomalyAndEventTests
{
    private AeroSimulator.Core.Aircraft.Aircraft CreateTestAircraft()
    {
        var aircraftPreset = DataPresets.AircraftPresets[0];
        var routePreset    = DataPresets.RoutePresets[0];
        var config         = new SimulationConfig(Difficulty.Normal, aircraftPreset, routePreset);
        var flightData     = new FlightData(config.Aircraft.EngineCount);
        var sensors        = new SensorSystem(config.Aircraft.EngineCount);

        return new AeroSimulator.Core.Aircraft.Aircraft(config, flightData, sensors);
    }

    private class EngineFireSpy : IFlightEventHandler
    {
        public bool EngineFireTriggered { get; private set; }

        public void Handle(FlightEvent evt)
        {
            if (evt is EngineFireEvent)
                EngineFireTriggered = true;
        }
    }

    // TEST 1 — WingFireAnomaly obniża WingHealth i aktywuje asymetryczny opór

    [Fact]
    public void WingFireAnomaly_Update_OdpalaAsymetrycznyOpor()
    {
        var aircraft = CreateTestAircraft();
        var anomaly  = new WingFireAnomaly();

        anomaly.Trigger(aircraft, aircraft.FlightData);


        for (int i = 0; i < 500; i++)
        {
            anomaly.Update(aircraft, aircraft.FlightData, 1.0);

            aircraft.DamageModel.WingHealth =
                Math.Max(0.0, aircraft.DamageModel.WingHealth - 0.01);

            aircraft.DamageModel.Update(1.0);

            if (aircraft.DamageModel.AsymmetricDragActive)
                break;
        }

        aircraft.DamageModel.AsymmetricDragActive
            .Should().BeTrue(
                "po osiągnięciu WingHealth < 0.20 DamageModel.Update() powinna ustawić AsymmetricDragActive = true");
    }

    // TEST 2 — WingFireAnomaly przy WingHealth = 0 kończy grę

    [Fact]
    public void WingFireAnomaly_Update_KonczyGreGdySkrzydloOdpada()
    {
        var aircraft = CreateTestAircraft();
        var anomaly  = new WingFireAnomaly();

        anomaly.Trigger(aircraft, aircraft.FlightData);

        for (int i = 0; i < 1000; i++)
        {
            anomaly.Update(aircraft, aircraft.FlightData, 1.0);

            aircraft.DamageModel.WingHealth =
                Math.Max(0.0, aircraft.DamageModel.WingHealth - 0.01);

            aircraft.DamageModel.Update(1.0);

            if (aircraft.DamageModel.IsGameOver)
                break;
        }

        aircraft.DamageModel.IsGameOver
            .Should().BeTrue(
                "po WingHealth = 0 DamageModel.Update() wywołuje TriggerGameOver() i IsGameOver = true");
    }

    // TEST 3 — ElectricalFailureAnomaly po 30 s wyłącza NavigationSystem

    [Fact]
    public void ElectricalFailureAnomaly_Update_KaskadaWylaczaNawigacje()
    {
        var aircraft = CreateTestAircraft();
        var anomaly  = new ElectricalFailureAnomaly();

        anomaly.Trigger(aircraft, aircraft.FlightData);

        for (int i = 0; i < 30; i++)
            anomaly.Update(aircraft, aircraft.FlightData, 1.0);

        aircraft.NavigationSystem.IsOffline
            .Should().BeTrue(
                "po 30 s bez prądu magistrala wtórna odpada i NavigationSystem powinien być offline");
    }

    // TEST 4 — CascadeHandler reaguje na BirdStrike i publikuje EngineFireEvent

    [Fact]
    public void CascadeHandler_BirdStrike_OdpalaPozarSilnika()
    {
        var aircraft       = CreateTestAircraft();
        var cascadeHandler = new CascadeHandler(aircraft);

        var spy = new EngineFireSpy();
        aircraft.Subscribe(spy);

        var birdStrikeEvent = new AnomalyTriggeredEvent(
            "BIRD_STRIKE", Severity.High, "Ptak uderzył w samolot");

        for (int attempt = 0; attempt < 50 && !spy.EngineFireTriggered; attempt++)
            cascadeHandler.Handle(birdStrikeEvent);

        spy.EngineFireTriggered
            .Should().BeTrue(
                "CascadeHandler powinien przechwycić uderzenie ptaka i opublikować EngineFireEvent");
    }
}