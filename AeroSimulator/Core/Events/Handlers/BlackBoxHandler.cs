using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AeroSimulator.Core.Events.Handlers;

public class BlackBoxHandler : IFlightEventHandler
{
    private static readonly List<FlightEvent> _recordedEvents = new();
    private static readonly object _lock = new();

    public static IReadOnlyList<FlightEvent> EventLog
    {
        get { lock (_lock) return _recordedEvents.ToArray(); }
    }

    public void Handle(FlightEvent evt)
    {
        lock (_lock)
        {
            _recordedEvents.Add(evt);
        }
    }

    public static void Clear()
    {
        lock (_lock) _recordedEvents.Clear();
    }

    // Zrzut pamięci do pliku .log na dysk (Podejście Deklaratywne / LINQ)
    public static void SaveToFile()
    {
        lock (_lock)
        {
            try
            {
                string directory = "Logs";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string filePath = Path.Combine(directory, $"blackbox_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                
                string header = 
                    "================================================================================\n" +
                   $"                  BLACKBOX FLIGHT DATA RECORDER DUMP\n" +
                   $"                  TIMESTAMP: {DateTime.Now}\n" +
                    "================================================================================\n";

                // 2. FUNKCYJNY POTOK DANYCH (LINQ Pipeline)
                // Zamiast pętli foreach, mapujemy (Select) każdy obiekt na string
                var logLines = _recordedEvents
                    .Select(evt => $"[{evt.Timestamp:HH:mm:ss.fff}] [{evt.Level.ToString().ToUpper()}] [{evt.Source.ToUpper()}] {evt.Message}");
                
                // 3. Agregacja (sklejanie) i zapis
                string fullContent = header + string.Join(Environment.NewLine, logLines);
                File.WriteAllText(filePath, fullContent);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n [!] SYSTEM WARNING: Failed to save blackbox log to disk.");
                Console.WriteLine($"     Reason: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}