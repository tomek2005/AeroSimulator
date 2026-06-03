namespace AeroSimulator.Infrastructure;

using System;
using AeroSimulator.Core.Aircraft;

public class FlightReport
{
    public static void PrintFinalReport(Aircraft aircraft)
    {
        Console.ResetColor();
        Console.WriteLine("\n==================================================");
        Console.WriteLine("                RAPORT KOŃCOWY LOTU               ");
        Console.WriteLine("==================================================");
        
        // Zmieniono AircraftModel na DisplayName, zgodnie z AircraftConfig
        Console.WriteLine($"Samolot:        {aircraft.Config?.DisplayName ?? "B737"} ({aircraft.EngineCount}x Engine)");
        
        // Wyciągamy TotalSeconds z obiektu TimeSpan
        Console.WriteLine($"Czas trwania:   {aircraft.FlightData.FlightTime.TotalSeconds:F1} sekund");
        Console.WriteLine($"Wysokość końc.: {aircraft.FlightData.Altitude:F0} ft");
        Console.WriteLine($"Prędkość końc.: {aircraft.FlightData.Speed:F1} kts");
        
        Console.WriteLine("--------------------------------------------------");
        if (aircraft.DamageModel.IsGameOver)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("STATUS LOTU: KATASTROFA (GAME OVER)");
            Console.WriteLine($"Powód:       {aircraft.DamageModel.GameOverReason}");
        }
        // Używamy .TotalSeconds przy sprawdzaniu czasu lotu
        else if (aircraft.CurrentState.StateName == "GROUND" && aircraft.FlightData.FlightTime.TotalSeconds > 10)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("STATUS LOTU: ZAKOŃCZONY SUKCESEM (Bezpieczne lądowanie)");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("STATUS LOTU: PRZERWANY / ZAKOŃCZONY W LOCIE");
        }
        Console.ResetColor();
        Console.WriteLine("==================================================\n");
    }
}