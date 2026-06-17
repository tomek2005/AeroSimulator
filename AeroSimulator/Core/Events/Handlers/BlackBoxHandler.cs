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
        get
        {
            lock (_lock) return _recordedEvents.ToArray();
        }
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

                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"                  BLACKBOX FLIGHT DATA RECORDER DUMP");
                sb.AppendLine($"                  TIMESTAMP: {DateTime.Now}");
                sb.AppendLine("================================================================================");

                foreach (var evt in _recordedEvents)
                {
                    sb.AppendLine(
                        $"[{evt.Timestamp:HH:mm:ss.fff}] [{evt.Level.ToString().ToUpper()}] [{evt.Source.ToUpper()}] {evt.Message}");
                }

                File.WriteAllText(filePath, sb.ToString());
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