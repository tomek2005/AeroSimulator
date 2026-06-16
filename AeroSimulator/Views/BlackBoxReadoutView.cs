using System;
using System.Threading;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Events.Handlers;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views;

public class BlackboxReadoutView : IScreen
{
    public string Title => "BLACK BOX READOUT";

    public void RenderHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine("================================================================================");
        Console.WriteLine($"                 [ DECRYPTING FLIGHT DATA RECORDER ({Title}) ]                ");
        Console.WriteLine("================================================================================");
        Console.ResetColor();
        Console.WriteLine(" Extracting telemetry and event logs...\n");
    }

    public void RenderMainContent()
    {
        Thread.Sleep(1500); // Dramatyczna pauza na początku (synchroniczna)

        var logs = BlackBoxHandler.EventLog;
        if (logs.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" [!] FLIGHT DATA RECORDER IS EMPTY.");
            Console.ResetColor();
        }
        else
        {
            foreach (var evt in logs)
            {
                // Znacznik czasu
                Console.Write($"[{evt.Timestamp:HH:mm:ss.fff}] ");
                
                // Kolorowanie w zależności od wagi zdarzenia
                if (evt.Level == Severity.Critical) Console.ForegroundColor = ConsoleColor.Red;
                else if (evt.Level == Severity.High) Console.ForegroundColor = ConsoleColor.DarkYellow;
                else if (evt.Level == Severity.Medium) Console.ForegroundColor = ConsoleColor.Yellow;
                else if (evt.Level == Severity.Info) Console.ForegroundColor = ConsoleColor.Cyan;
                else Console.ForegroundColor = ConsoleColor.DarkGray;

                // Źródło i treść wiadomości
                Console.Write($"[{evt.Source.ToUpper()}] ");
                Console.ResetColor();
                Console.WriteLine(evt.Message);

                // DRAMATYCZNY WYDRUK:
                // Normalna telemetria przelatuje bardzo szybko (20ms).
                // Kiedy dochodzi do pożaru lub kaskady (Critical/High), wydruk znacząco zwalnia (400ms).
                if (evt.Level == Severity.Critical || evt.Level == Severity.High)
                    Thread.Sleep(400); 
                else
                    Thread.Sleep(20);  
            }
        }
    }

    public void RenderFooter()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("                           [ END OF RECORDING ]                                 ");
        Console.WriteLine("================================================================================");
        Console.ResetColor();
        
        // --- ZASTOSOWANIE PARADYGMATU FUNKCYJNEGO ---
        // Wywołanie Czystej Funkcji, która przetwarza logi (LINQ) i zwraca niezmienny rekord
        var stats = StatisticsHandler.GenerateReport(BlackBoxHandler.EventLog);
        
        // Zwieńczenie punktu o statystykach po locie
        Console.WriteLine("\n [ FLIGHT SUMMARY ]");
        Console.WriteLine($" Total Anomalies:     {stats.TotalAnomalies}");
        Console.WriteLine($" Cascades Triggered:  {stats.TotalCascades}");
        Console.WriteLine($" System Failures:     {stats.TotalFailures}");
        Console.WriteLine($" Flight Phases (St):  {stats.StateTransitions}");
        Console.WriteLine($" Critical Alerts:     {stats.CriticalAlerts}");
        
        Console.WriteLine("\n Press any key to return to Main Menu...");
    }

    public void RenderAll()
    {
        RenderHeader();
        RenderMainContent();
        RenderFooter();
    }

    public void HandleInput(ConsoleKey key)
    {
        // W tym widoku wejście jest tylko jedno (wyjście), obsługiwane bezpośrednio po RenderAll
    }
}