using System.Diagnostics;
using AeroSimulator.Core.Events;

namespace AeroSimulator.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.States;
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
    private readonly Random _rng = new();
    private IWeatherStrategy _weatherStrategy;
    private double _weatherChangeCountdownSec;

    private IScreen _activeView;
    private bool _isRunning = true;
    private bool _hasBeenAirborne;
    private bool _landedSafely;
    private bool _autoLandingActive;

    // --- NOWE ZMIENNE DO OBSŁUGI PAUZY ---
    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

    public FlightController(Aircraft aircraft, IScreen dashboard, IScreen camera, SimulationConfig config)
    {
        _aircraft = aircraft;
        _dashboardView = dashboard;
        _cameraView = camera;
        _config = config;
        
        _activeView = _dashboardView; 
        _anomalyEngine = new AnomalyEngine(aircraft);
        _weatherStrategy = PickWeatherForDifficulty();
        _weatherChangeCountdownSec = RollWeatherDuration();
        _aircraft.WeatherSystem.SetCurrentCondition(_weatherStrategy.Name, _weatherChangeCountdownSec);
        _inputHandler = new InputHandler(this, aircraft, _anomalyEngine);
    }

    public void ToggleView()
    {
        Console.Clear(); 
        _activeView = _activeView == _dashboardView ? _cameraView : _dashboardView;
    }

    // --- NOWA METODA DO PAUZOWANIA ---
    public void TogglePause()
    {
        _isPaused = !_isPaused;
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
            
            // 1. ZAWSZE Obsługa wejścia gracza (nawet na pauzie!)
            _inputHandler.ProcessInput();

            // 2. LOGIKA GRY ORAZ FIZYKA (Tylko gdy NIE ma pauzy)
            if (!_isPaused)
            {
                double verticalSpeedBeforeUpdate = _aircraft.FlightData.VerticalSpeed;
                double speedBeforeUpdate = _aircraft.FlightData.Speed;
                double pitchBeforeUpdate = _aircraft.FlightData.PitchAngleDeg;
                double rollBeforeUpdate = _aircraft.FlightData.RollAngleDeg;
                bool gearExtendedBeforeUpdate = _aircraft.HydraulicSystem.IsGearExtended;

                // Aktualizacja systemów i fizyki
                UpdateWeather(deltaT);
                EnforceFlightEnvelope();
                _aircraft.Update(deltaT);
                AutoManageFlightPhase();
                ApplyAutoLandingAssist(deltaT);
                _anomalyEngine.Tick(deltaT);

                if (_aircraft.FlightData.Altitude > 50.0)
                {
                    _hasBeenAirborne = true;
                }

                if (_hasBeenAirborne
                    && _aircraft.FlightData.Altitude <= 0
                    && !_aircraft.DamageModel.IsGameOver)
                {
                    EvaluateTouchdown(
                        gearExtendedBeforeUpdate,
                        speedBeforeUpdate,
                        verticalSpeedBeforeUpdate,
                        pitchBeforeUpdate,
                        rollBeforeUpdate);
                }

                // 4. Generowanie telemetrii do Czarnej Skrzynki (Co 1 sekundę)
                tickCounter++;
                if (tickCounter >= 10)
                {
                    var fd = _aircraft.FlightData;
                    _aircraft.Publish(new TelemetryTickEvent($"ALT: {fd.Altitude:0}ft | SPD: {fd.Speed:0}kts | HDG: {fd.Heading:0}° | PITCH: {fd.PitchAngleDeg:F1}°"));
                    tickCounter = 0;
                }
            }

            // 5. ZAWSZE Renderowanie aktywnego widoku na ekran
            Console.SetCursorPosition(0, 0); 
            _activeView.RenderAll();

            // Dodatkowy interfejs podczas pauzy
            if (_isPaused)
            {
                Console.SetCursorPosition(38, 15); // Wyśrodkowanie na standardowym ekranie
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.Write("  [ SIMULATION PAUSED ]  ");
                Console.ResetColor();
            }

            // 6. Stabilizacja czasu klatki (10Hz)
            stopwatch.Stop();
            int timeToWait = delayMs - (int)stopwatch.ElapsedMilliseconds;
            if (timeToWait > 0)
            {
                await Task.Delay(timeToWait);
            }
        }
        
        // --- PO ZAKOŃCZENIU PĘTLI ---
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

    public void NotifyLandingPhaseIsAutomatic()
    {
        _aircraft.PublishAlert("LANDING PHASE IS AUTOMATIC - use [Y] below 650 ft for auto-land assist", Severity.Info);
    }

    public void TryStartAutoLanding()
    {
        var fd = _aircraft.FlightData;
        if (_aircraft.AutopilotSystem.IsOffline)
        {
            _aircraft.PublishAlert("AUTO-LAND UNAVAILABLE - autopilot offline", Severity.High);
            return;
        }

        if (fd.Altitude > 650.0)
        {
            _aircraft.PublishAlert("AUTO-LAND ARMED ONLY BELOW 650 FT AGL", Severity.Info);
            return;
        }

        _autoLandingActive = true;
        _aircraft.HydraulicSystem.EmergencyGearExtension();
        _aircraft.WingSystem.SetFlaps(1.0);
        SetState(new LandingState());
        _aircraft.PublishAlert("AUTO-LAND ENGAGED - aircraft will stabilize final approach", Severity.Info);
    }

    private void UpdateWeather(double deltaT)
    {
        _weatherChangeCountdownSec -= deltaT;
        if (_weatherChangeCountdownSec <= 0)
        {
            _weatherStrategy = PickWeatherForDifficulty();
            _weatherChangeCountdownSec = RollWeatherDuration();
            _aircraft.PublishAlert($"WEATHER UPDATE: {_weatherStrategy.Name}", Severity.Info);
        }

        _weatherStrategy.Apply(_aircraft, deltaT);
        _aircraft.WeatherSystem.SetCurrentCondition(_weatherStrategy.Name, _weatherChangeCountdownSec);
    }

    private IWeatherStrategy PickWeatherForDifficulty()
    {
        string[] pool = _config.Difficulty switch
        {
            Difficulty.Easy => ["CLEAR", "CLEAR", "FOG", "CROSSWIND"],
            Difficulty.Normal => ["CLEAR", "FOG", "CROSSWIND", "ICING", "THUNDERSTORM"],
            _ => ["CROSSWIND", "ICING", "THUNDERSTORM", "WINDSHEAR", "WINDSHEAR"]
        };

        return WeatherFactory.CreateWeather(pool[_rng.Next(pool.Length)]);
    }

    private double RollWeatherDuration() => 35.0 + _rng.NextDouble() * 55.0;

    private void EvaluateTouchdown(bool gearExtended, double speedKts, double verticalSpeedFtMin, double pitchDeg, double rollDeg)
    {
        double descentRate = Math.Abs(Math.Min(verticalSpeedFtMin, 0.0));
        double absPitch = Math.Abs(pitchDeg);
        double absRoll = Math.Abs(rollDeg);

        double maxSpeed = gearExtended ? 250.0 : 150.0;
        double maxDescentRate = gearExtended ? 900.0 : 350.0;
        double maxPitch = gearExtended ? 12.0 : 5.0;
        double maxRoll = 3.0;

        bool stableApproach = speedKts <= maxSpeed
                              && descentRate <= maxDescentRate
                              && absPitch <= maxPitch
                              && absRoll <= maxRoll
                              && _aircraft.CurrentState.StateName is "LANDING" or "GROUND";

        if (stableApproach)
        {
            _landedSafely = true;
            _isRunning = false;
            _aircraft.Publish(new LandingCompletedEvent(
                gearExtended,
                speedKts,
                verticalSpeedFtMin,
                pitchDeg,
                $"SAFE TOUCHDOWN ({(gearExtended ? "GEAR DOWN" : "BELLY LANDING")}) - {speedKts:F0} kt, VS {verticalSpeedFtMin:F0} ft/min, pitch {pitchDeg:F1} deg, roll {rollDeg:F1} deg."));
            return;
        }

        string gearText = gearExtended ? "gear-down" : "gear-up";
        string reason = $"{gearText} touchdown unstable: speed {speedKts:F0}/{maxSpeed:F0} kt, descent {descentRate:F0}/{maxDescentRate:F0} ft/min, pitch {absPitch:F1}/{maxPitch:F1} deg, roll {absRoll:F1}/{maxRoll:F1} deg";
        _aircraft.Publish(new SystemFailureEvent("HULL", 0.0, $"CRASH LANDING - {reason}"));
        _aircraft.DamageModel.TriggerGameOver(reason);
        _aircraft.Publish(new GameOverEvent(reason));
    }

    private void AutoManageFlightPhase()
    {
        if (_aircraft.DamageModel.IsGameOver) return;
        if (_aircraft.CurrentState.StateName is "EMERGENCY" or "CRITICAL") return;

        var fd = _aircraft.FlightData;
        string state = _aircraft.CurrentState.StateName;

        if (!_hasBeenAirborne && fd.Altitude <= 5.0)
        {
            if (fd.Speed <= 3.0)
            {
                SetState(new GroundState());
                return;
            }

            if (fd.Speed < _config.Aircraft.VRSpeedKts * 0.75)
            {
                SetState(new TaxiState());
                return;
            }

            SetState(new TakeOffState());
            return;
        }

        if (fd.Altitude > 80.0)
        {
            _hasBeenAirborne = true;
        }

        if (!_hasBeenAirborne) return;

        if (fd.Altitude <= 1_200.0 && (fd.VerticalSpeed < -80.0 || state is "DESCENT" or "LANDING"))
        {
            SetState(new LandingState());
            return;
        }

        if (fd.VerticalSpeed < -250.0 || fd.PitchAngleDeg < -3.0)
        {
            SetState(new DescentState());
            return;
        }

        double ceilingBandFt = 3_300.0;
        bool nearCeiling = _config.Aircraft.MaxAltitudeFt - fd.Altitude <= ceilingBandFt;
        if ((fd.Altitude > 5_000.0 && Math.Abs(fd.PitchAngleDeg) <= 3.0)
            || nearCeiling)
        {
            SetState(new CruiseState());
            return;
        }

        if (fd.VerticalSpeed > 250.0 || fd.PitchAngleDeg > 3.0)
        {
            SetState(new ClimbState());
        }
    }

    private void ApplyAutoLandingAssist(double deltaT)
    {
        if (!_autoLandingActive || _aircraft.DamageModel.IsGameOver) return;

        var fd = _aircraft.FlightData;
        if (fd.Altitude > 800.0)
        {
            _autoLandingActive = false;
            _aircraft.PublishAlert("AUTO-LAND DISENGAGED - aircraft climbed above capture altitude", Severity.Info);
            return;
        }

        _aircraft.HydraulicSystem.EmergencyGearExtension();
        _aircraft.WingSystem.SetFlaps(1.0);
        SetState(new LandingState());

        double targetSpeed = fd.Altitude > 80.0 ? 185.0 : 155.0;
        fd.Throttle = fd.Speed > targetSpeed ? 0.0 : 0.22;
        fd.Speed += (targetSpeed - fd.Speed) * 0.65 * deltaT;

        if (fd.Altitude > 80.0)
        {
            fd.PitchAngleDeg = -1.5;
            fd.VerticalSpeed = -280.0;
        }
        else
        {
            fd.PitchAngleDeg = 2.0;
            fd.VerticalSpeed = -140.0;
        }

        fd.Altitude = Math.Max(0.0, fd.Altitude + (fd.VerticalSpeed / 60.0) * deltaT);
    }

    private void EnforceFlightEnvelope()
    {
        var fd = _aircraft.FlightData;
        double distanceToCeilingFt = _config.Aircraft.MaxAltitudeFt - fd.Altitude;

        if (distanceToCeilingFt <= 3_300.0)
        {
            double blend = Math.Clamp(distanceToCeilingFt / 3_300.0, 0.0, 1.0);
            double maxPositivePitch = 3.0 + 9.0 * blend;
            fd.PitchAngleDeg = Math.Min(fd.PitchAngleDeg, maxPositivePitch);
        }

        fd.RollAngleDeg = Math.Clamp(fd.RollAngleDeg, -30.0, 30.0);
    }

    private void SetState(IAircraftState state)
    {
        if (_aircraft.CurrentState.StateName == state.StateName) return;
        _aircraft.TransitionTo(state);
    }
}
