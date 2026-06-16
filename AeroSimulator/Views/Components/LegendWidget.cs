using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class LegendWidget : IWidget
{
    public void Render()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n----------------------------------------------------------------------------------------------------");
        Console.WriteLine(" KONTROLA: [W] Nos w dol | [S] Nos w gore | [A/D] Kurs -/+5 deg | [↑/↓] Ciag | [U] Cofnij");
        Console.WriteLine(" LOT:      [T] Start reczny | [L] Zejscie/Ladowanie | [Y] Auto-land <650ft | [Spacja] Go-around | [P] Pauza");
        Console.WriteLine(" SYSTEMY:  [O] Silniki | [B] Szyna glowna | [H] Hydraulika | [G] Podwozie awaryjnie | [Z] Autopilot");
        Console.WriteLine(" SKRZYDLA: [C] Spoilery | [1] Klapy dol | [2] Klapy gora | [I] Odladzanie");
        Console.WriteLine(" AWARIE:   [E] Emergency | [R] Napraw anomalie | [X] Gasnice | [K] Uszczelnij wyciek | [V] Widok | [Esc] Wyjscie");
        Console.WriteLine("----------------------------------------------------------------------------------------------------");
        Console.ResetColor();
    }
}
