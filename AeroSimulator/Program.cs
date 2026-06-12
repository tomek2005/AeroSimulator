using System;
using AeroSimulator.Controllers;
using AeroSimulator.Infrastructure;
using AeroSimulator.Views;
using AeroSimulator.Views.Components;

public class Program
{
    // ZMIANA: Dodano 'async Task' aby umożliwić działanie operatora 'await'
    public static async Task Main(string[] args)
    {
        // 1. Konfiguracja konsoli (najpierw, zanim cokolwiek narysujemy)
        Console.CursorVisible = false;
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "AeroSim Flight Engine";
        
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Console.WindowWidth = 100;
                Console.WindowHeight = 35;
            }
        }
        catch 
        {
            // Ignorujemy błąd, jeśli środowisko nie wspiera zmiany rozmiaru
        }

        // 2. Faza 1: Ekran Powitalny (Splash Screen)
        var splashScreen = new IntroSplashScreen();
        await splashScreen.ShowAsync();

        // 3. Faza 2: Ekran Startowy z wstrzykniętymi zależnościami (Dependency Injection)
        var startupScreen = new StartupScreen(
            DataPresets.AircraftPresets, 
            DataPresets.RoutePresets
        );
        
        // ZMIANA: Używamy nowej, asynchronicznej metody cyklu życia ekranu
        await startupScreen.RunScreenLifecycleAsync();

        // 4. Faza 3: Przejście do właściwej gry (gdy wyjdziemy z menu)
        if (startupScreen.IsConfigurationFinished)
        {
            // 1. POBIERAMY JEDYNE ŹRÓDŁO PRAWDY
            SimulationConfig config = startupScreen.FinalConfig;
            
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("====================================================================");
            Console.WriteLine("  AEROSIM CORE ENGINE - INICJALIZACJA WYBRANEJ KONFIGURACJI");
            Console.WriteLine("====================================================================");
            Console.ResetColor();
            
            // 2. CZYTAMY ZMIENNE BEZPOŚREDNIO Z 'config'
            Console.WriteLine($" Załadowany model: {config.Aircraft.DisplayName}");
            Console.WriteLine($" Trasa:            {config.Route.Name}");
            Console.WriteLine($" Poziom trudności: {config.Difficulty}");
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n  Uruchamianie systemów pokładowych i kalibracja czujników...");
            Console.ResetColor();

            // 3. CZEKAMY 4 SEKUNDY (Symulacja ładowania gry)
            await Task.Delay(4000);

            
            // 4. BUDUJEMY MODEL PRZEZ FABRYKĘ (Wzorzec Factory)
            // Tworzy główny obiekt statku powietrznego na podstawie configu
            var aircraftModel = AircraftFactory.Create(config);
            
            var dashboardView = new ConsoleDashboardView(aircraftModel);
            var cameraView = new CameraView(aircraftModel);
            
            // 3. Inicjalizacja kontrolera i start pętli 10Hz
            var flightController = new FlightController(aircraftModel, dashboardView, cameraView, config);

            Console.Clear();
            await flightController.StartSimulationLoopAsync();

            Console.Clear();
            FlightReportView.PrintFinalReport(
                aircraftModel.FlightData.Snapshot(),
                aircraftModel.Sensors.GetAllSensors(),
                aircraftModel.Config.Aircraft.DisplayName,
                aircraftModel.EngineCount,
                aircraftModel.DamageModel.IsGameOver,
                aircraftModel.DamageModel.GameOverReason,
                aircraftModel.CurrentState.StateName);
            Console.WriteLine("Naciśnij dowolny klawisz, aby przejść do czarnej skrzynki...");
            Console.ReadKey(true);
            
            AeroSimulator.Core.Events.Handlers.BlackBoxHandler.SaveToFile();
            
// 6. FAZA PO-LOTU (Menu Blackbox / Wyjście)
            bool showPostGameMenu = true;
            while (showPostGameMenu)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n====================================================================");
                Console.WriteLine("                     [ SIMULATION TERMINATED ]                      ");
                Console.WriteLine("====================================================================");
                Console.ResetColor();
                
                Console.WriteLine("\n Session ended. Logs have been committed to Black Box storage.");
                Console.WriteLine("\n Choose an action:");
                Console.WriteLine(" [F] Show Final Flight Report");
                Console.WriteLine(" [R] Read Black Box Data (Dramatic Printout)");
                Console.WriteLine(" [Q] Quit to Desktop");
                
                Console.Write("\n > ");
                var keyInfo = Console.ReadKey(true);
                
                if (keyInfo.Key == ConsoleKey.F)
                {
                    Console.Clear();
                    FlightReportView.PrintFinalReport(
                        aircraftModel.FlightData.Snapshot(),
                        aircraftModel.Sensors.GetAllSensors(),
                        aircraftModel.Config.Aircraft.DisplayName,
                        aircraftModel.EngineCount,
                        aircraftModel.DamageModel.IsGameOver,
                        aircraftModel.DamageModel.GameOverReason,
                        aircraftModel.CurrentState.StateName);
                    Console.ReadKey(true);
                }
                else if (keyInfo.Key == ConsoleKey.R)
                {
                    var readoutView = new BlackboxReadoutView();
                    readoutView.RenderAll(); 
                    Console.ReadKey(true);
                }
                else if (keyInfo.Key == ConsoleKey.Q || keyInfo.Key == ConsoleKey.Escape)
                {
                    showPostGameMenu = false;
                }
            }

            // Ostateczne czyszczenie konsoli przed zamknięciem
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Thank you for using AeroSim. Shutting down...");
            Console.ResetColor();
            await Task.Delay(1000);
        }
    }
}
