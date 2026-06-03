namespace AeroSimulator.Core.Aircraft;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.States;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Events.Handlers;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Aircraft.Systems;

public class Aircraft
{
    private IAircraftState _currentState;

    public IAircraftState CurrentState => _currentState;

    private EventBus _eventBus;

    // Słownik jest teraz na starcie pusty, wypełnimy go automatycznie w konstruktorze
    private readonly Dictionary<SystemType, double> _systemsHealth = new();

    // Systemy, które zależą od liczby silników, deklarujemy tutaj, a inicjalizujemy w konstruktorze
    public AircraftSensors Sensors { get; private set; }
    public FlightData FlightData { get; private set; }
    public EngineSystem EngineSystem { get; private set; }
    public AircraftConfig Config { get; private set; }
    public int EngineCount => EngineSystem.EngineCount;

    // Pozostałe, "stałe" systemy
    public AutopilotSystem AutopilotSystem { get; } = new AutopilotSystem();
    public ElectricalSystem ElectricalSystem { get; } = new ElectricalSystem();
    public HydraulicSystem HydraulicSystem { get; } = new HydraulicSystem();
    public FuelSystem FuelSystem { get; } = new FuelSystem();
    public NavigationSystem NavigationSystem { get; } = new NavigationSystem();
    public DamageModel DamageModel { get; } = new DamageModel();

    public Aircraft(string tailNumber, string model, AircraftConfig config)
    {
        Config = config;
        // 1. POBIERAMY LICZBĘ SILNIKÓW Z KONFIGURACJI
        int numEngines = (config != null && config.EngineCount > 0) ? config.EngineCount : 2;

        // 2. TWORZYMY SYSTEMY ZALEŻNE OD LICZBY SILNIKÓW
        EngineSystem = new EngineSystem(numEngines);
        Sensors = new AircraftSensors(numEngines);
        FlightData = new FlightData(numEngines);

        // 3. AUTOMATYCZNIE WYPEŁNIAMY ZDROWIE SYSTEMÓW AWIONIKI (z Enuma)
        foreach (SystemType sys in Enum.GetValues(typeof(SystemType)))
        {
            _systemsHealth[sys] = 1.0;
        }

        // 4. Inicjalizacja EventBusa i reszty systemu (Twój stary kod)
        _eventBus = EventBus.Instance;

        var cascadeHandler = new CascadeHandler(this);
        Subscribe(cascadeHandler);
        
        Subscribe(new BlackBoxHandler());
        Subscribe(new AlertSystemHandler());
        Subscribe(new FlightLoggerHandler());
        Subscribe(new StatisticsHandler());
        
        _currentState = new GroundState(); 
        _currentState.OnEnter(this);
    }

    // Nowa metoda do pobierania silnika (zwraca teraz EngineUnit z EngineSystem)
    public EngineUnit GetEngine(int index)
    {
        return EngineSystem.GetEngine(index);
    }

    /// <summary>
    /// Odpowiada za bezpieczną zmianę stanu samolotu, zarządzanie jego cyklem życia
    /// oraz powiadamianie systemu o zmianie fazy lotu.
    /// </summary>
    public void TransitionTo(IAircraftState newState)
    {
        if (newState == null)
        {
            throw new ArgumentNullException(nameof(newState), "Nowy stan nie może być nullem.");
        }

        string oldStateName = _currentState?.StateName ?? "None";
        _currentState?.OnExit(this);

        _currentState = newState;
        _currentState.OnEnter(this);

        Publish(new StateChangedEvent
        {
            Timestamp = DateTime.Now,
            Source = "Aircraft",
            Level = Severity.Info,
            Message = $"State transitioned from [{oldStateName}] to [{_currentState.StateName}]",
            OldState = oldStateName,
            NewState = _currentState.StateName
        });
    }
    /// <summary>
    /// Wywoływane przez anomalie do wysyłania szybkich powiadomień tekstowych do EventBusa.
    /// </summary>
    public void PublishAlert(string message, Severity level)
    {
        // Pakujemy powiadomienie w zdarzenie (wymaga istnienia AnomalyTriggeredEvent w systemie zdarzeń)
        Publish(new AnomalyTriggeredEvent("GeneralAnomaly", level.ToString(), message));
    }

    /// <summary>
    /// Wymóg kaskadowego systemu uszkodzeń. Pozwala jednej anomalii natychmiast 
    /// wywołać kolejną.
    /// </summary>
    public void ForceSpawnAnomaly(AeroSimulator.Core.Strategies.Anomalies.IAnomaly cascadeAnomaly)
    {
        Publish(new SystemFailureEvent(cascadeAnomaly.AnomalyName, 1.0, $"CASCADE FAILURE TRIGGERED: {cascadeAnomaly.GetWarningMessage()}"));
    }

    public void TakeOff() => _currentState.TakeOff(this);
    
    public void Cruise() => _currentState.Cruise(this);
    
    public void Descend() => _currentState.Descend(this);

    public void Land() => _currentState.Land(this);
    public void DeclareEmergency() => _currentState.HandleEmergency(this);
    public void Abort() => _currentState.Abort(this);

    public void Update(double deltaT) => _currentState.Update(this, deltaT);

    // BARDZO WAŻNE: Metody dla EventBusa, by błąd `Subscribe/Publish` zniknął
    public void Subscribe(IFlightEventHandler handler) => _eventBus.Subscribe(handler);
    public void Publish(FlightEvent evt) => _eventBus.Publish(evt);

    // Metody do zarządzania zdrowiem reszty systemów (tych bez silników, np. Hydraulika, Paliwo z SystemType)
    public double GetSystemHealth(SystemType system)
    {
        return _systemsHealth.TryGetValue(system, out double health) ? health : 1.0;
    }

    public void ApplyDamage(SystemType system, double damageAmount)
    {
        if (_systemsHealth.ContainsKey(system))
        {
            _systemsHealth[system] = Math.Max(0.0, _systemsHealth[system] - damageAmount);
            
            Publish(new SystemFailureEvent(
                system.ToString(), 
                1.0 - _systemsHealth[system], 
                $"System {system} damaged by {damageAmount * 100:0.#}%"));
        }
    }
}