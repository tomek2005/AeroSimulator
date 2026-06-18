using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Core.Aircraft.Sensors;

namespace AeroSimulator.Views;

public static class FlightReportView
{
    public static void PrintFinalReport(
        FlightDataSnapshot dataSnapshot,
        IReadOnlyList<ISensor> sensors,
        string aircraftName,
        int engineCount,
        bool isGameOver,
        string gameOverReason,
        string finalStateName)
    {
        Console.ResetColor();
        Console.WriteLine("\n==================================================");
        Console.WriteLine("                RAPORT KOŃCOWY LOTU               ");
        Console.WriteLine("==================================================");

        Console.WriteLine($"Samolot:        {aircraftName} ({engineCount}x Engine)");
        Console.WriteLine($"Czas trwania:   {dataSnapshot.FlightTime.TotalSeconds:F1} sekund");
        Console.WriteLine($"Wysokość końc.: {dataSnapshot.Altitude:F0} ft");
        Console.WriteLine($"Prędkość końc.: {dataSnapshot.Speed:F1} kts");

        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(" STAN CZUJNIKÓW AWIONIKI:");

        foreach (var sensor in sensors)
        {
            Console.ForegroundColor = sensor.State switch
            {
                SensorState.OK => ConsoleColor.DarkGreen,
                SensorState.Noisy => ConsoleColor.Yellow,
                SensorState.Fault => ConsoleColor.Magenta,
                SensorState.Dead => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
            Console.WriteLine($"  {sensor.SensorName,-15} {sensor.State,-8} (Sprawność: {sensor.Accuracy * 100:0}%)");
        }

        Console.ResetColor();

        Console.WriteLine("--------------------------------------------------");
        if (isGameOver)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("STATUS LOTU: KATASTROFA (GAME OVER)");
            Console.WriteLine($"Powód:       {gameOverReason}");
        }
        else if (finalStateName == "GROUND" && dataSnapshot.FlightTime.TotalSeconds > 10)
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