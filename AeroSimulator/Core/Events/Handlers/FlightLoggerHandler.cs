using System;
using System.IO;

namespace AeroSimulator.Core.Events.Handlers;

public class FlightLoggerHandler : IFlightEventHandler
{
    private readonly string _logFilePath = "flight_progress.log";

    public FlightLoggerHandler()
    {
        // Nadpisujemy plik tekstowy przy każdym nowym uruchomieniu symulatora
        try
        {
            File.WriteAllText(_logFilePath, $"=== LOG ROZPOCZĘCIA SYMULACJI: {DateTime.Now} ==={Environment.NewLine}");
        }
        catch
        {
            // zabezpieczenie
        }
    }

    public void Handle(FlightEvent evt)
    {
        string logLine = $"[{evt.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{evt.Level}] [{evt.Source}] {evt.Message}";
        
        try
        {
            File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
        }
        catch
        {
            // again
        }
    }
}