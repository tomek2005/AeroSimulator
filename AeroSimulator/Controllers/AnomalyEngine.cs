namespace AeroSimulator.Controllers;

using System;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;

public class AnomalyEngine
{
    private readonly Random _random = new();
    private readonly double _probability;
    private bool _anomalyActive = false;
    private AnomalyDefinition? _currentAnomaly;

    public AnomalyEngine(double probabilityPerSec)
    {
        _probability = probabilityPerSec;
    }

    public void Update(Aircraft aircraft, double deltaT)
    {
        // Jeżeli nie ma aktywnej anomalii, losujemy jej wystąpienie (tylko gdy samolot jest w powietrzu)
        if (!_anomalyActive && aircraft.CurrentState.StateName != "GROUND")
        {
            // Dostosowanie prawdopodobieństwa do kroku czasowego deltaT
            if (_random.NextDouble() < (_probability * deltaT))
            {
                _currentAnomaly = AnomalyFactory.GetRandomAnomaly();
                _anomalyActive = true;
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n!!! AWARIA W LOCIE: {_currentAnomaly.WarningMessage} !!!");
                Console.ResetColor();

                // Automatycznie wymuszamy przejście samolotu w stan awaryjny!
                aircraft.DeclareEmergency();
            }
        }

        // Jeśli anomalia trwa, pogarszamy parametry strukturalne i symulujemy asymetryczny opór
        if (_anomalyActive && _currentAnomaly != null)
        {
            // Losowo aktywujemy asymetryczne znoszenie kadłuba (np. od uszkodzonego skrzydła/silnika)
            aircraft.DamageModel.AsymmetricDragActive = true;
            aircraft.DamageModel.DriftDegPerSec = 2.5; // Znoszenie o 2.5 stopnia na sekundę

            // Co jakiś czas sprawdzamy krytyczne zmęczenie materiału, które może wywołać bezpośredni Game Over
            if (_random.NextDouble() < 0.02 * deltaT)
            {
                aircraft.DamageModel.TriggerGameOver($"Struktura uległa dezintegracji z powodu: {_currentAnomaly.AnomalyName}");
            }
        }
    }
}