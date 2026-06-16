using AeroSimulator.Core.Aircraft.Sensors;
using AeroSimulator.Infrastructure;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.States;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Events.Handlers;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Core.Aircraft;

//// <summary>
/// GŁÓWNY MODEL (wzorzec MVC). Klasa skupiająca systemy pokładowe, stany (State Pattern) 
/// oraz parametry lotu. Agreguje komponenty i deleguje logikę lotu do aktualnego stanu.
/// </summary>
public class Aircraft
{
    // ==========================================
    // STAN (STATE PATTERN)
    // ==========================================
    private IAircraftState _currentState;
    public IAircraftState CurrentState => _currentState;

    // ==========================================
    // DANE I SYSTEMY (Wstrzyknięte z Fabryki)
    // ==========================================
    public SimulationConfig Config { get; }
    public FlightData FlightData { get; }
    public SensorSystem Sensors { get; }
    
    // ==========================================
    // PODSYSTEMY LOKALNE
    // ==========================================
    public EngineSystem EngineSystem { get; }
    public AutopilotSystem AutopilotSystem { get; } = new();
    public ElectricalSystem ElectricalSystem { get; } = new();
    public HydraulicSystem HydraulicSystem { get; } = new();
    public FuelSystem FuelSystem { get; } = new();
    public NavigationSystem NavigationSystem { get; } = new();
    public WingSystem WingSystem { get; } = new();
    public WeatherSystem WeatherSystem { get; } = new();
    
    public DamageModel DamageModel { get; } 

    public int EngineCount => EngineSystem.EngineCount;

    private readonly Dictionary<SystemType, double> _systemsHealth = new();
    private readonly EventBus _eventBus;
    private bool _gameOverEventPublished;

    // ==========================================
    // KONSTRUKTOR WSTRZYKUJĄCY (Dependency Injection)
    // ==========================================
    public Aircraft(SimulationConfig config, FlightData initialData, SensorSystem sensors)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        FlightData = initialData ?? throw new ArgumentNullException(nameof(initialData));
        Sensors = sensors ?? throw new ArgumentNullException(nameof(sensors));

        EngineSystem = new EngineSystem(config.Aircraft.EngineCount);
        DamageModel = new DamageModel(config.Aircraft.EngineCount);

        foreach (SystemType sys in Enum.GetValues(typeof(SystemType)))
        {
            _systemsHealth[sys] = 1.0; 
        }

        _eventBus = EventBus.Instance;
        _eventBus.ClearHandlers();
        BlackBoxHandler.Clear();
        AlertBufferHandler.Clear();
        StatisticsHandler.Reset();
        
        Subscribe(new CascadeHandler(this));
        Subscribe(new BlackBoxHandler()); 
        Subscribe(new AlertSystemHandler()); // Jeśli masz swój własny, zostawiamy
        Subscribe(new FlightLoggerHandler());
        Subscribe(new StatisticsHandler());
        Subscribe(new AlertBufferHandler());
        
        _currentState = new GroundState(); 
        _currentState.OnEnter(this);
    }

    public void Update(double deltaT)
    {
        FlightData.FlightTime += TimeSpan.FromSeconds(deltaT);

        _currentState.Update(this, deltaT);
        DamageModel.Update(deltaT);
        PublishGameOverOnceIfNeeded();
        AutopilotSystem.Update(FlightData, Sensors, deltaT);

        // 1. LIMIT PRZEPUSTNICY (0% - 100%)
        FlightData.Throttle = Math.Clamp(FlightData.Throttle, 0.0, 1.0);

        double engineEfficiency = CalculateEngineEfficiency();
        if (FlightData.FuelLevelKg <= 0 && !EngineSystem.IsOffline)
        {
            EngineSystem.SetOffline();
            DeclareEmergency();
            Publish(new SystemFailureEvent("FuelSystem", 0.0, "FUEL EXHAUSTED - all engines flamed out."));
        }

        // 2. OBLICZANIE PRĘDKOŚCI Z LIMITAMI Z KONFIGURACJI SAMOLOTU I REALNYM CIĄGIEM
        double availableThrust = FlightData.FuelLevelKg > 0 ? engineEfficiency : 0.0;
        double targetSpeed = FlightData.Throttle * Config.Aircraft.MaxSpeedKts * availableThrust;
        double responseRate = availableThrust > 0 ? 0.20 : 0.08;
        FlightData.Speed += (targetSpeed - FlightData.Speed) * responseRate * deltaT;
        FlightData.Speed = Math.Clamp(FlightData.Speed, 0.0, Config.Aircraft.MaxSpeedKts);

        // 3. FIZYKA WZNOSZENIA (Siła nośna)
        double stallSpeed = Config.Aircraft.StallSpeedKts + FlightData.StallSpeedOffset;
        if (FlightData.Speed > stallSpeed)
        {
            // Prędkość pionowa zależy od pochylenia (Pitch) i aktualnej prędkości
            FlightData.VerticalSpeed = FlightData.PitchAngleDeg * (FlightData.Speed / 100.0) * 100.0;

            if (availableThrust <= 0.05 && FlightData.Altitude > 0)
            {
                FlightData.VerticalSpeed = Math.Min(FlightData.VerticalSpeed, -900.0);
                FlightData.PitchAngleDeg = Math.Min(FlightData.PitchAngleDeg, 2.0);
            }
        }
        else
        {
            FlightData.VerticalSpeed = 0; // Za wolno by lecieć w górę
        
            // Jeśli jesteśmy w powietrzu, ale zwolniliśmy poniżej prędkości przeciągnięcia - spadamy
            if (FlightData.Altitude > 0) 
            {
                FlightData.VerticalSpeed = -1500.0; // Szybki spadek
                FlightData.PitchAngleDeg = -10.0;   // Nos opada
            }
        }

        // 4. ZMIANA WYSOKOŚCI I LIMIT ZIEMI
        FlightData.Altitude += (FlightData.VerticalSpeed / 60.0) * deltaT;
    
        if (FlightData.Altitude <= 0)
        {
            FlightData.Altitude = 0;
            FlightData.VerticalSpeed = 0;
            // Na ziemi kąt pochylenia wymuszamy na płaski (chyba, że przy starcie ciągniemy w górę)
            if (FlightData.Speed <= stallSpeed) FlightData.PitchAngleDeg = 0; 
        }

        // Limit pułapu z konfiguracji
        FlightData.Altitude = Math.Min(FlightData.Altitude, Config.Aircraft.MaxAltitudeFt);

        if (DamageModel.AsymmetricDragActive)
        {
            FlightData.ApplyAsymmetricDrift(DamageModel.DriftDegPerSec, deltaT);
            FlightData.AsymmetricDrag = Math.Max(FlightData.AsymmetricDrag, DamageModel.DriftDegPerSec / 5.0);
        }

        // 5. ZUŻYCIE PALIWA
        if (FlightData.FuelLevelKg > 0)
        {
            double throttleBurnMultiplier = 0.2 + 1.8 * FlightData.Throttle;
            double baseBurn = (Config.Aircraft.FuelBurnKgPerH / 3600.0) * throttleBurnMultiplier * engineEfficiency * deltaT;
            double leakBurn = (FuelSystem.CurrentLeakRate / 3600.0) * deltaT;
            FlightData.FuelLevelKg = Math.Max(0, FlightData.FuelLevelKg - baseBurn - leakBurn);
            FlightData.FuelFlowKgPerH = Config.Aircraft.FuelBurnKgPerH * throttleBurnMultiplier * engineEfficiency + FuelSystem.CurrentLeakRate;
        }
        else
        {
            FlightData.FuelFlowKgPerH = 0;
        }

        UpdateEngineReadings(deltaT);

        // 6. Aktualizacja czujników
        Sensors.Update(deltaT, FlightData);
        Sensors.UpdateHydraulicReading(HydraulicSystem.Pressure);
    }

    private void UpdateEngineReadings(double deltaT)
    {
        for (int i = 0; i < EngineCount; i++)
        {
            var engine = EngineSystem.GetEngine(i);
            if (engine.IsOnFire)
            {
                engine.ApplyDamage(0.02 * deltaT);
                DamageModel.SetEngineFireState(i, FireState.Burning);

                if (engine.Health <= 0 && !DamageModel.IsExploded)
                {
                    engine.Explode(this, FlightData);
                    Sensors.EngineRPMs[i].Kill();
                    Publish(new EngineExplosionEvent(i, $"Engine {i + 1} exploded after uncontrolled fire."));
                    DamageModel.TriggerExplosion();
                    PublishGameOverOnceIfNeeded();
                }
            }

            double rpmTarget = engine.IsRunning && FlightData.FuelLevelKg > 0 && engine.Health > 0
                ? FlightData.Throttle * engine.Health * 100.0
                : 0.0;
            FlightData.EngineRPMs[i] += (rpmTarget - FlightData.EngineRPMs[i]) * 0.35;
            FlightData.EngineTempsC[i] = engine.IsOnFire
                ? Math.Min(1100.0, FlightData.EngineTempsC[i] + 120.0 * deltaT)
                : 15.0 + FlightData.EngineRPMs[i] * 7.0;
        }
    }

    private double CalculateEngineEfficiency()
    {
        if (EngineCount <= 0 || EngineSystem.IsOffline) return 0.0;

        double total = 0.0;
        for (int i = 0; i < EngineCount; i++)
        {
            var engine = EngineSystem.GetEngine(i);
            total += engine.IsRunning ? engine.Health : 0.0;
        }

        return Math.Clamp(total / EngineCount, 0.0, 1.0);
    }

    private void PublishGameOverOnceIfNeeded()
    {
        if (!DamageModel.IsGameOver || _gameOverEventPublished) return;

        _gameOverEventPublished = true;
        Publish(new GameOverEvent(DamageModel.GameOverReason));
    }

    // ==========================================
    // ZARZĄDZANIE STANEM
    // ==========================================
    public void TakeOff() => _currentState.TakeOff(this);
    public void Cruise() => _currentState.Cruise(this);
    public void Descend() => _currentState.Descend(this);
    public void Land() => _currentState.Land(this);
    public void DeclareEmergency() => _currentState.HandleEmergency(this);
    public void Abort() => _currentState.Abort(this);

    public void TransitionTo(IAircraftState newState)
    {
        if (newState == null) throw new ArgumentNullException(nameof(newState));

        string oldStateName = _currentState?.StateName ?? "None";
        _currentState?.OnExit(this);

        _currentState = newState;
        _currentState.OnEnter(this);

        Publish(new StateChangedEvent(
            oldStateName,
            _currentState.StateName,
            $"State transitioned from [{oldStateName}] to [{_currentState.StateName}]"
        ));
    }

    // ==========================================
    // ZARZĄDZANIE SYSTEMAMI I ZDROWIEM
    // ==========================================
    public EngineUnit GetEngine(int index) => EngineSystem.GetEngine(index);

    public double GetSystemHealth(SystemType system) => 
        _systemsHealth.TryGetValue(system, out double health) ? health : 1.0;

    public void ApplyDamage(SystemType system, double damageAmount)
    {
        if (_systemsHealth.ContainsKey(system))
        {
            _systemsHealth[system] = Math.Max(0.0, _systemsHealth[system] - damageAmount);
            
            Publish(new SystemFailureEvent(
                system.ToString(), 
                _systemsHealth[system], // Przekazujemy obecne zdrowie (Health) zgodnie z klasą zdarzenia
                $"System {system} damaged by {damageAmount * 100:0.#}%"));
        }
    }

    public void Subscribe(IFlightEventHandler handler) => _eventBus.Subscribe(handler);
    public void Publish(FlightEvent evt) => _eventBus.Publish(evt);

    public void PublishAlert(string message, Severity level)
    {
        Publish(new AnomalyTriggeredEvent("GeneralAnomaly", level, message));
    }
}