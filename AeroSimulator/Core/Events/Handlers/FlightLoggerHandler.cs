using System;
using System.IO;

namespace AeroSimulator.Core.Events.Handlers;

public class FlightLoggerHandler : IFlightEventHandler
{
    private readonly string _logFilePath;

    public FlightLoggerHandler()
    {
        string directory = "Logs";
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        _logFilePath = Path.Combine(directory, $"flight_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    }

    public void Handle(FlightEvent evt)
    {
        // Plik loguje tylko czystą telemetrię i błędy systemowe
        if (evt is PlayerInputEvent || evt is CommandExecutedEvent) return;

        string logLine = $"[{evt.Timestamp:HH:mm:ss}] [{evt.Level}] [{evt.Source}] {evt.Message}";
        try { File.AppendAllText(_logFilePath, logLine + Environment.NewLine); } catch { }
    }
}