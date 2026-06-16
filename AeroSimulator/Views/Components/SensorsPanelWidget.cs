using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class SensorsPanelWidget : IWidget
{
    private readonly Aircraft _aircraft;

    public SensorsPanelWidget(Aircraft aircraft) => _aircraft = aircraft;

    public void Render()
    {
        Console.WriteLine("\n[ SENSOR STATUS ]");

        var sensors = _aircraft.Sensors.GetAllSensors();
        int rendered = 0;

        foreach (var sensor in sensors)
        {
            Console.ForegroundColor = sensor.State switch
            {
                Core.Aircraft.Enums.SensorState.OK => ConsoleColor.Green,
                Core.Aircraft.Enums.SensorState.Noisy => ConsoleColor.Yellow,
                Core.Aircraft.Enums.SensorState.Fault => ConsoleColor.Red,
                Core.Aircraft.Enums.SensorState.Dead => ConsoleColor.DarkRed,
                _ => ConsoleColor.Gray
            };

            Console.Write($"{sensor.SensorName}:{sensor.State}({sensor.Accuracy * 100:F0}%)".PadRight(24));
            Console.ResetColor();

            rendered++;
            if (rendered % 3 == 0) Console.WriteLine();
        }

        if (rendered % 3 != 0) Console.WriteLine();
    }
}
