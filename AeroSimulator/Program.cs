using System;
using AeroSimulator.Controllers;
using AeroSimulator.Infrastructure;
using AeroSimulator.Views;
using AeroSimulator.Views.Components;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Terminal config
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

        }

        // Game setup menu
        var splashScreen = new IntroSplashScreen();
        await splashScreen.ShowAsync();
        
        var startupScreen = new StartupScreen(
            DataPresets.AircraftPresets,
            DataPresets.RoutePresets
        );
        
        await startupScreen.RunScreenLifecycleAsync();

        // Game loop
        if (startupScreen.IsConfigurationFinished)
        {
            SimulationConfig config = startupScreen.FinalConfig;

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("====================================================================");
            Console.WriteLine("  AEROSIM CORE ENGINE - INICJALIZACJA WYBRANEJ KONFIGURACJI");
            Console.WriteLine("====================================================================");
            Console.ResetColor();
            
            Console.WriteLine($" Airplane model: {config.Aircraft.DisplayName}");
            Console.WriteLine($" Route:            {config.Route.Name}");
            Console.WriteLine($" Difficulty: {config.Difficulty}");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n Uruchamianie systemów pokładowych i kalibracja czujników...");
            Console.ResetColor();
            
            await Task.Delay(4000);

            
            var aircraftModel = AircraftFactory.Create(config);

            var dashboardView = new ConsoleDashboardView(aircraftModel);
            var cameraView = new CameraView(aircraftModel);
            
            var flightController = new FlightController(aircraftModel, dashboardView, cameraView, config);

            Console.Clear();
            await flightController.StartSimulationLoopAsync();

            
            // End game stats redout
            Console.Clear();
            FlightReportView.PrintFinalReport(
                aircraftModel.FlightData.Snapshot(),
                aircraftModel.Sensors.GetAllSensors(),
                aircraftModel.Config.Aircraft.DisplayName,
                aircraftModel.EngineCount,
                aircraftModel.DamageModel.IsGameOver,
                aircraftModel.DamageModel.GameOverReason,
                aircraftModel.CurrentState.StateName);
            Console.WriteLine("Click any key to read blackbox data...");
            Console.ReadKey(true);

            AeroSimulator.Core.Events.Handlers.BlackBoxHandler.SaveToFile();

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
            
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Thank you for using AeroSim. Shutting down...");
            Console.ResetColor();
            await Task.Delay(1000);
        }
    }
}