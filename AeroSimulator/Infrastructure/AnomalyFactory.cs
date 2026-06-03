namespace AeroSimulator.Infrastructure;

using System;

public class AnomalyFactory
{
    private static readonly Random _random = new();

    public static AnomalyDefinition GetRandomAnomaly()
    {
        string[] names = { "Bird Strike", "Wing Fire", "Hydraulic Leak", "Engine Flameout" };
        string[] msgs = { "Zderzenie z ptakami!", "Pożar lewego skrzydła!", "Wyciek płynu hydraulicznego!", "Zgaśnięcie silnika nr 1!" };
        
        int index = _random.Next(names.Length);
        
        return new AnomalyDefinition
        {
            AnomalyName = names[index],
            WarningMessage = msgs[index],
            SeverityIncreaseRate = 0.05 + (_random.NextDouble() * 0.1)
        };
    }
}

// Prosta klasa pomocnicza reprezentująca cechy anomalii, dopasowana do Twojego Aircraft.cs
public class AnomalyDefinition
{
    public string AnomalyName { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public double SeverityIncreaseRate { get; set; }
    public string GetWarningMessage() => WarningMessage;
}