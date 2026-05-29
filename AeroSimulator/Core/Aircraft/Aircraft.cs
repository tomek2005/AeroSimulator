namespace AeroSimulator.Core.Aircraft;

using System;
using System.Collections.Generic;
using AeroSimulator.Core.States;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Aircraft.Enums;

public class Aircraft
{
    // 1. Prywatne pole przechowujące aktualny stan
    private IAircraftState _currentState;

    // Publiczna właściwość umożliwiająca innym modułom (np. Widokom) odczyt stanu
    public IAircraftState CurrentState => _currentState;

    // Pozostałe pola i systemy zdefiniowane w specyfikacji
    // private FlightData _flightData;
    // private EventBus _eventBus;
    // ...

    public Aircraft(string tailNumber, string model, AircraftConfig config)
    {
        // Inicjalizacja systemów, danych lotu, szyny zdarzeń itp.
        // _eventBus = EventBus.Instance;
        
        // Ustawienie stanu początkowego na GroundState zgodnie ze specyfikacją
        _currentState = new GroundState(); 
        _currentState.OnEnter(this);
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

        // a) Wywołanie OnExit na dotychczasowym stanie (jeśli istnieje)
        string oldStateName = _currentState?.StateName ?? "None";
        _currentState?.OnExit(this);

        // b) Zmiana referencji na nowy stan
        _currentState = newState;

        // c) Wywołanie OnEnter na nowym stanie
        _currentState.OnEnter(this);

        // d) Wysłanie StateChangedEvent na EventBus (Singleton)
        EventBus.Instance.Publish(new StateChangedEvent
        {
            Timestamp = DateTime.Now,
            Source = "Aircraft",
            Level = Severity.Info,
            Message = $"State transitioned from [{oldStateName}] to [{_currentState.StateName}]",
            OldState = oldStateName,
            NewState = _currentState.StateName
        });
    }

    // 3. Delegacja akcji pilota bezpośrednio do aktualnego stanu

    public void TakeOff() => _currentState.TakeOff(this);
    
    public void Cruise() => _currentState.Cruise(this);
    
    public void Descend() => _currentState.Descend(this);
    
    public void Land() => _currentState.Land(this);
    
    public void DeclareEmergency() => _currentState.HandleEmergency(this);
    
    public void Abort() => _currentState.Abort(this);

    /// <summary>
    /// Główna metoda aktualizująca stan samolotu w pętli 10 Hz.
    /// </summary>
    public void Update(double deltaT)
    {
        // Tutaj najpierw wykonuje się ogólna fizyka i aktualizacja systemów autonomicznych samolotu:
        // _engine1.Update(deltaT, _flightData);
        // _sensors.Update(deltaT, _flightData);
        // _damageModel.Update(deltaT);
        // Aplikowanie asymetrycznego znoszenia itd.

        // Następnie delegujemy specyficzną dla danej fazy lotu logikę do aktualnego stanu
        _currentState.Update(this, deltaT);
    }
}