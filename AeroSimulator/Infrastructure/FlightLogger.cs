namespace AeroSimulator.Infrastructure;

using System;
using System.IO;
using AeroSimulator.Core.Aircraft;

public class FlightLogger
{
    private readonly string _filePath;

    public FlightLogger(string filePath)
    {
        _filePath = filePath;
        InitializeFile();
    }

    private void InitializeFile()
    {
        // Tworzy nagłówki w pliku CSV przy uruchomieniu
        string headers = "Timestamp,Altitude,Speed,VerticalSpeed,Heading,Throttle,EngineRPMs,FuelLevelKg";
        File.WriteAllText(_filePath, headers + Environment.NewLine);
    }

    public void LogData(FlightData flightData)
    {
        try
        {
            // Korzystamy z Twojego dedykowanego rekordu snapshotu!
            var snapshot = new FlightDataSnapshot(flightData);
            File.AppendAllText(_filePath, snapshot.ToCsvRow() + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Logger Error] Nie udało się zapisać telemetrii: {ex.Message}");
        }
    }
}