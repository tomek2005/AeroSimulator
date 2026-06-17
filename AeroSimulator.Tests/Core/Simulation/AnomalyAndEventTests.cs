using System;
using Xunit;
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
    // ── Fabryka testowego samolotu ────────────────────────────────────────────
    private AeroSimulator.Core.Aircraft.Aircraft CreateTestAircraft()
    {
        var aircraftPreset = DataPresets.AircraftPresets[0];   // Boeing 737-800
        var routePreset    = DataPresets.RoutePresets[0];

        // Kolejność: (Difficulty, Aircraft, Route)
        var config     = new SimulationConfig(Difficulty.Normal, aircraftPreset, routePreset);
        var flightData = new FlightData(config.Aircraft.EngineCount);
        var sensors    = new SensorSystem(config.Aircraft.EngineCount);

        return new AeroSimulator.Core.Aircraft.Aircraft(config, flightData, sensors);
    }

    // ── Spy rejestrujący EngineFireEvent na EventBus ──────────────────────────
    private class EngineFireSpy : IFlightEventHandler
    {
        public bool EngineFireTriggered { get; private set; }

        public void Handle(FlightEvent evt)
        {
            if (evt is EngineFireEvent)
                EngineFireTriggered = true;
        }
    }

    /// <summary>
    /// Deterministyczny "fałszywy" Random — NextDouble() zawsze zwraca 0.0,
    /// Next() zawsze zwraca minValue. Gwarantuje spełnienie warunku &lt; 0.40
    /// w CascadeHandler niezależnie od wersji .NET czy platformy.
    /// </summary>
    private sealed class AlwaysZeroRandom : Random
    {
        public override double NextDouble() => 0.0;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    // =========================================================================
    // TEST 1 — WingFireAnomaly obniża WingHealth i aktywuje asymetryczny opór
    // =========================================================================
    [Fact]
    public void WingFireAnomaly_Update_OdpalaAsymetrycznyOpor()
    {
        var aircraft = CreateTestAircraft();
        var anomaly  = new WingFireAnomaly();

        anomaly.Trigger(aircraft, aircraft.FlightData);

        // 1 s na tick × maks. 500 iteracji = 500 s symulacji.
        // Decay = 0.01/s → po ~50 tickach WingHealth osiągnie 0.5.
        for (int i = 0; i < 500; i++)
        {
            anomaly.Update(aircraft, aircraft.FlightData, 1.0);
            if (aircraft.DamageModel.WingHealth <= 0.5)
                break;
        }

        aircraft.DamageModel.AsymmetricDragActive
            .Should().BeTrue(
                "po osiągnięciu WingHealth ≤ 0.5 anomalia powinna ustawić AsymmetricDragActive = true");
    }

    // =========================================================================
    // TEST 2 — WingFireAnomaly przy WingHealth = 0 kończy grę
    // =========================================================================
    [Fact]
    public void WingFireAnomaly_Update_KonczyGreGdySkrzydloOdpada()
    {
        var aircraft = CreateTestAircraft();
        var anomaly  = new WingFireAnomaly();

        anomaly.Trigger(aircraft, aircraft.FlightData);

        // 0.01/s × 100 s = 1.0 całego zdrowia → maks. 1000 ticków z zapasem
        for (int i = 0; i < 1000; i++)
        {
            anomaly.Update(aircraft, aircraft.FlightData, 1.0);
            if (aircraft.DamageModel.WingHealth <= 0.0)
                break;
        }

        aircraft.DamageModel.IsGameOver
            .Should().BeTrue(
                "po całkowitym zniszczeniu skrzydła (WingHealth = 0) gra powinna się skończyć");
    }

    // =========================================================================
    // TEST 3 — ElectricalFailureAnomaly po 30 s wyłącza NavigationSystem
    // =========================================================================
    [Fact]
    public void ElectricalFailureAnomaly_Update_KaskadaWylaczaNawigacje()
    {
        var aircraft = CreateTestAircraft();
        var anomaly  = new ElectricalFailureAnomaly();

        anomaly.Trigger(aircraft, aircraft.FlightData);

        // Dokładnie 30 ticków po 1 s = 30 s symulacji
        for (int i = 0; i < 30; i++)
            anomaly.Update(aircraft, aircraft.FlightData, 1.0);

        aircraft.NavigationSystem.IsOffline
            .Should().BeTrue(
                "po 30 s bez prądu magistrala wtórna odpada i NavigationSystem powinien być offline");
    }

    // =========================================================================
    // TEST 4 — CascadeHandler reaguje na BirdStrike i publikuje EngineFireEvent
    // =========================================================================
    [Fact]
    public void CascadeHandler_BirdStrike_OdpalaPozarSilnika()
    {
        var aircraft = CreateTestAircraft();

        // AlwaysZeroRandom: NextDouble() = 0.0 < 0.40 → warunek kaskady ZAWSZE spełniony,
        // niezależnie od seedów i wersji .NET.
        var fakeRng        = new AlwaysZeroRandom();
        var cascadeHandler = new CascadeHandler(aircraft, fakeRng);

        // Spy nasłuchuje EngineFireEvent na EventBus samolotu
        var spy = new EngineFireSpy();
        aircraft.Subscribe(spy);

        var birdStrikeEvent = new AnomalyTriggeredEvent(
            "BIRD_STRIKE", Severity.High, "Ptak uderzył w samolot");

        cascadeHandler.Handle(birdStrikeEvent);

        spy.EngineFireTriggered
            .Should().BeTrue(
                "CascadeHandler powinien przechwycić uderzenie ptaka i opublikować EngineFireEvent");
    }
}