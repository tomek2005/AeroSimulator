using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class LegendWidget : IWidget
{
    public void Render()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n----------------------------------------------------------------------------------------------------");
        Console.WriteLine(" KONTROLA: [W/S] Pochylenie | [A/D] Przechylenie | [↑/↓] Ciąg Silników | [U] Cofnij");
        Console.WriteLine(" AKCJE:    [T] Start | [L] Lądowanie | [E] Sytuacja Awaryjna | [Spacja] Przerwanie (Abort)");
        Console.WriteLine(" SYSTEMY:  [O] Silniki      | [B] Szyna Główna | [H] Hydraulika | [G] Podwozie | [V] Widok");
        Console.WriteLine(" SKRZYDŁA: [C] Spoilery     | [1] Klapy Dół  | [2] Klapy Góra | [I] Odladzanie");
        Console.WriteLine(" AWARYJNE: [R] Rozwiąż Anomalię | [X] Gaśnice | [K] Uszczelnij Wyciek | [Z] Autopilot");
        Console.WriteLine("----------------------------------------------------------------------------------------------------");
        Console.ResetColor();
    }
}
