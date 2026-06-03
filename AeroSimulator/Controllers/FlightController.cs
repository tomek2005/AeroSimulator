namespace AeroSimulator.Controllers;

using System;
using System.Threading;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;

public class FlightController
{
    private readonly SimulationConfig _config;
    private readonly FlightLogger _logger;
    private readonly InputHandler _inputHandler;
    private readonly AnomalyEngine _anomalyEngine;

    public FlightController()
    {
        _config = new SimulationConfig();
        _logger = new FlightLogger(_config.LogFilePath);
        _inputHandler = new InputHandler();
        _anomalyEngine = new AnomalyEngine(_config.AnomalyProbabilityPerSecond);
    }

    public void StartSimulation()
    {
        // 1. Inicjalizacja obiektów
        Aircraft aircraft = AircraftFactory.CreateBoeing737("SP-FLY");
    
        Console.Clear();
        Console.WriteLine($"=== URUCHOMIONO SYMULATOR LOTU AEROSIMULATOR ===");
        Console.WriteLine($"Zarejestrowany statek: {aircraft.Config?.DisplayName ?? "B737"} [Reg: SP-FLY]");
        InputHandler.PrintControls();

        var weather = WeatherFactory.GenerateDynamicWeather();
        aircraft.FlightData.WindSpeedKnots = weather.WindSpeed;
        Console.WriteLine($"Pogoda: Wiatr {weather.WindSpeed:F0} węzłów, Turbulencje: {(weather.Turbulence ? "TAK" : "NIE")}");
        Console.WriteLine("Naciśnij [T], aby rozpocząć rozbieg na pasie...");
        Console.WriteLine("=================================================\n");

        string lastStateName = "";
        int lastPrintedSecond = -1; // Zmienna zapobiegająca spamowaniu konsoli

        // 2. Główna Pętla Gry
        while (!aircraft.DamageModel.IsGameOver && aircraft.FlightData.FlightTime.TotalSeconds < _config.TotalSimulationTimeLimit)
        {
            double dt = _config.TimeStepDeltaT;
            aircraft.FlightData.FlightTime += TimeSpan.FromSeconds(dt);

            _inputHandler.ProcessInput(aircraft);
            _anomalyEngine.Update(aircraft, dt);
            aircraft.Update(dt);
            
            // --- 0. SPALANIE PALIWA ---
            if (aircraft.FlightData.FuelLevelKg > 0)
            {
                // Pobieramy bazowe spalanie przelotowe (ok. 2500 kg/h dla B737) i dzielimy na sekundy
                double baseBurnPerSec = (aircraft.Config?.FuelBurnKgPerH ?? 2500.0) / 3600.0;
                
                // Mnożnik spalania: na jałowym biegu (0% mocy) pali tylko 20% bazy. 
                // Przy 100% mocy na start pali aż 200% (2x) tego co w przelocie.
                double currentBurnRate = baseBurnPerSec * (0.2 + 1.8 * aircraft.FlightData.Throttle);
                
                // Odejmujemy paliwo na podstawie upływającego czasu (dt)
                aircraft.FlightData.FuelLevelKg -= currentBurnRate * dt;
                
                // Zabezpieczenie przed ujemnym wynikiem
                if (aircraft.FlightData.FuelLevelKg < 0) aircraft.FlightData.FuelLevelKg = 0;
            }

            // --- 1. SPRAWNOŚĆ SILNIKÓW ---
            double totalEngineHealth = 0.0;
            for (int i = 0; i < aircraft.EngineCount; i++)
            {
                totalEngineHealth += aircraft.EngineSystem.GetEngine(i).Health;
            }
            double engineEfficiency = totalEngineHealth / aircraft.EngineCount;

            // KRYTYCZNE ZABEZPIECZENIE: Brak paliwa = brak ciągu silników!
            if (aircraft.FlightData.FuelLevelKg <= 0)
            {
                engineEfficiency = 0.0;
                
                // Jeśli komputer jeszcze nie jest w trybie awaryjnym, to go włączamy!
                if (aircraft.CurrentState.StateName != "EMERGENCY" && aircraft.CurrentState.StateName != "CRITICAL")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n!!! KRYTYCZNA AWARIA: BRAK PALIWA (FUEL EXHAUSTION) - ZGASNIĘCIE SILNIKÓW !!!");
                    Console.ResetColor();
                    
                    // Automatyczne zgłoszenie Mayday i zmiana stanu komputera pokładowego
                    aircraft.DeclareEmergency(); 
                }
            }

            // --- 2. SIŁA CIĄGU --- (zależy od pozycji przepustnicy oraz zdrowia silników)
            double maxAcceleration = 12.0; 
            double thrustForce = aircraft.FlightData.Throttle * maxAcceleration * engineEfficiency;

            // --- 3. OPÓR DYNAMICZNY ---
            double dragCoefficient = 0.04; 
            double dragForce = aircraft.FlightData.Speed * dragCoefficient;

            // --- 4. WYLICZENIE PRZYSPIESZENIA WYPADKOWEGO ---
            double netAcceleration = thrustForce - dragForce;
            aircraft.FlightData.Speed = Math.Max(0.0, aircraft.FlightData.Speed + (netAcceleration * dt));

            // --- 5. PŁYNNE WZNOSZENIE ---
            if (aircraft.CurrentState.StateName != "GROUND" && aircraft.CurrentState.StateName != "TAXI")
            {
                if (aircraft.FlightData.Speed > 130) 
                {
                    double climbRate = (aircraft.FlightData.Speed - 110) * 1.5; 
                    aircraft.FlightData.Altitude += climbRate * dt;
                }
                else
                {
                    // Przeciągnięcie (spadanie poniżej bezpiecznej prędkości)
                    aircraft.FlightData.Altitude = Math.Max(0.0, aircraft.FlightData.Altitude - 50.0 * dt);
                }
            }
            // =================================================================

            _logger.LogData(aircraft.FlightData);

            // Wyciągamy sekundy jako liczbę całkowitą
            int currentSecond = (int)aircraft.FlightData.FlightTime.TotalSeconds;

            // Warunek drukuje HUD tylko RAZ na 5 sekund lub przy zmianie stanu
            if (aircraft.CurrentState.StateName != lastStateName || (currentSecond % 5 == 0 && currentSecond > 0 && currentSecond != lastPrintedSecond))
            {
                Console.ForegroundColor = aircraft.CurrentState.StateColor;
                Console.WriteLine($"[{currentSecond}s] STAN: {aircraft.CurrentState.StateName} -> ALT: {aircraft.FlightData.Altitude:F0} ft | SPD: {aircraft.FlightData.Speed:F0} kts | HDG: {aircraft.FlightData.Heading:F0}° | Fuel: {aircraft.FlightData.FuelLevelKg:F0} kg");
                Console.ResetColor();
            
                lastStateName = aircraft.CurrentState.StateName;
                lastPrintedSecond = currentSecond; // Zapamiętujemy, że tę sekundę już wypisaliśmy
            }

            if (_config.RealTimeSimulation)
            {
                Thread.Sleep((int)(dt * 1000));
            }
        }

        // 3. Koniec symulacji
        FlightReport.PrintFinalReport(aircraft);
    }
}