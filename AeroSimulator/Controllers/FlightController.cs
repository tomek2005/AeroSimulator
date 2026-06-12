using System.Diagnostics;
using AeroSimulator.Core.Events;

namespace AeroSimulator.Controllers;

using System;
using System.Threading;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Strategies.Weather;
using AeroSimulator.Infrastructure;

public class FlightController
{
    private readonly Aircraft _aircraft;
    private readonly IScreen _dashboardView;
    private readonly IScreen _cameraView;
    private readonly SimulationConfig _config;
    private readonly InputHandler _inputHandler;
    private readonly AnomalyEngine _anomalyEngine;
    private readonly IWeatherStrategy _weatherStrategy;

    private IScreen _activeView;
    private bool _isRunning = true;
    private bool _hasBeenAirborne;
    private bool _landedSafely;

    public FlightController(Aircraft aircraft, IScreen dashboard, IScreen camera, SimulationConfig config)
    {
        _aircraft = aircraft;
        _dashboardView = dashboard;
        _cameraView = camera;
        _config = config;
        
        _activeView = _dashboardView; 
        _anomalyEngine = new AnomalyEngine(aircraft);
        _weatherStrategy = config.Difficulty switch
        {
            Core.Aircraft.Enums.Difficulty.Easy => WeatherFactory.CreateWeather("CLEAR"),
            Core.Aircraft.Enums.Difficulty.Normal => WeatherFactory.CreateWeather("CROSSWIND"),
            _ => WeatherFactory.CreateWeather("WINDSHEAR")
        };
        _inputHandler = new InputHandler(this, aircraft, _anomalyEngine);
    }

    public void ToggleView()
    {
        Console.Clear(); 
        _activeView = _activeView == _dashboardView ? _cameraView : _dashboardView;
    }

    public async Task StartSimulationLoopAsync()
    {
        double deltaT = _config.TimeStepDeltaT; // Domyślnie 0.1s
        int delayMs = (int)(deltaT * 1000);     // Oczekiwany czas klatki (100ms)
        int tickCounter = 0;

        // Pętla kręci się dopóki gracz nie wyjdzie (isRunning) ORAZ samolot nie ulegnie zniszczeniu (!IsGameOver)
        while (_isRunning && !_aircraft.DamageModel.IsGameOver)
        {
            var stopwatch = Stopwatch.StartNew();
            double verticalSpeedBeforeUpdate = _aircraft.FlightData.VerticalSpeed;

            // 1. Obsługa wejścia gracza
            _inputHandler.ProcessInput();

            // 2. Aktualizacja systemów i fizyki
            _weatherStrategy.Apply(_aircraft, deltaT);
            _aircraft.Update(deltaT);
            _anomalyEngine.Tick(deltaT);

            if (_aircraft.FlightData.Altitude > 50.0)
            {
                _hasBeenAirborne = true;
            }

            // ==========================================
            // 3. SPRAWDZENIE KATASTROFY (Twarde lądowanie / Uderzenie w ziemię)
            // ==========================================
            // Jeśli samolot jest na ziemi (Altitude <= 0) i spadał zbyt szybko (VerticalSpeed < -500 FT/MIN)
            if (_aircraft.FlightData.Altitude <= 0 && verticalSpeedBeforeUpdate < -500.0)
            {
                if (!_aircraft.DamageModel.IsGameOver)
                {
                    // Publikujemy krytyczne wydarzenie do Czarnej Skrzynki
                    _aircraft.Publish(new SystemFailureEvent("HULL", 0.0, "IMPACT WITH TERRAIN! CATASTROPHIC HULL FAILURE!"));
                    
                    // Uruchamiamy API Twojego modelu - to spowoduje IsGameOver = true i przerwie pętlę!
                    _aircraft.DamageModel.TriggerGameOver("Fatal impact with terrain.");
                    _aircraft.Publish(new GameOverEvent("Fatal impact with terrain."));
                }
            }

            if (_hasBeenAirborne
                && _aircraft.FlightData.Altitude <= 0
                && _aircraft.CurrentState.StateName == "GROUND"
                && !_aircraft.DamageModel.IsGameOver)
            {
                _landedSafely = true;
                _isRunning = false;
                _aircraft.Publish(new SystemFailureEvent("Flight", 1.0, "SAFE LANDING - flight cycle completed."));
            }

            // 4. Generowanie telemetrii do Czarnej Skrzynki (Co 1 sekundę)
            tickCounter++;
            if (tickCounter >= 10)
            {
                var fd = _aircraft.FlightData;
                _aircraft.Publish(new TelemetryTickEvent($"ALT: {fd.Altitude:0}ft | SPD: {fd.Speed:0}kts | HDG: {fd.Heading:0}° | PITCH: {fd.PitchAngleDeg:F1}°"));
                tickCounter = 0;
            }

            // 5. Renderowanie aktywnego widoku na ekran
            Console.SetCursorPosition(0, 0); 
            _activeView.RenderAll();

            // 6. Stabilizacja czasu klatki (10Hz)
            stopwatch.Stop();
            int timeToWait = delayMs - (int)stopwatch.ElapsedMilliseconds;
            if (timeToWait > 0)
            {
                await Task.Delay(timeToWait);
            }
        }
        
        // --- PO ZAKOŃCZENIU PĘTLI ---
        // Pętla się zatrzymała! Zanim wrócimy do pliku Program.cs, dajemy graczowi 2.5 sekundy,
        // żeby na spokojnie przeczytał, dlaczego gra się skończyła.
        if (_aircraft.DamageModel.IsGameOver)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n\n [!] SYSTEM HALTED: {_aircraft.DamageModel.GameOverReason}");
            Console.ResetColor();
            await Task.Delay(2500); 
        }
    }

    public void Quit() => _isRunning = false;
    public bool LandedSafely => _landedSafely;
}
