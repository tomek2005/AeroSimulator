namespace AeroSimulator.Controllers;

using System;
using AeroSimulator.Core.Aircraft;

public class InputHandler
{
    public void ProcessInput(Aircraft aircraft)
    {
        if (!Console.KeyAvailable) return;

        var key = Console.ReadKey(intercept: true).Key;

        switch (key)
        {
            case ConsoleKey.W: // Zwiększ przepustnicę (tylko sterowanie ciągiem)
                aircraft.FlightData.Throttle = Math.Min(1.0, aircraft.FlightData.Throttle + 0.1);
                Console.WriteLine($"[Input] Przepustnica: {aircraft.FlightData.Throttle * 100:F0}%");
                break;

            case ConsoleKey.S: // Zmniejsz przepustnicę
                aircraft.FlightData.Throttle = Math.Max(0.0, aircraft.FlightData.Throttle - 0.1);
                Console.WriteLine($"[Input] Przepustnica: {aircraft.FlightData.Throttle * 100:F0}%");
                break;

            case ConsoleKey.T: // Start (TakeOff)
                aircraft.TakeOff();
                break;

            case ConsoleKey.A: // Abort (Przerwanie startu)
                aircraft.Abort();
                break;

            case ConsoleKey.L: // Lądowanie / Awaryjne lądowanie
                aircraft.Land();
                break;

            case ConsoleKey.E: // Deklaracja sytuacji awaryjnej (Mayday)
                aircraft.DeclareEmergency();
                break;

            default:
                break;
        }
    }

    public static void PrintControls()
    {
        Console.WriteLine("\n--- STEROWANIE ---");
        Console.WriteLine("[W] / [S] - Zwiększ / Zmniejsz Ciąg (Throttle)");
        Console.WriteLine("[T] - Prośba o kołowanie / Start (TakeOff)");
        Console.WriteLine("[A] - Przerwanie procedury startowej (Abort)");
        Console.WriteLine("[L] - Lądowanie (Land)");
        Console.WriteLine("[E] - Wywołanie stanu awaryjnego (MAYDAY)");
        Console.WriteLine("-------------------\n");
    }
}