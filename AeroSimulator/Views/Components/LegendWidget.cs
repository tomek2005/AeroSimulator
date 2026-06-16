using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class LegendWidget : IWidget
{
    public void Render()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n----------------------------------------------------------------------------------------------------");
        Console.WriteLine(" KONTROLA: [W] Nos w dol | [S] Nos w gore | [A/D] Roll -/+2 deg | [0] Poziomuj | [↑/↓] Ciag | [U] Cofnij");
        Console.WriteLine(" LOT:      Fazy automatyczne | [Y] Auto-land <650ft | [Spacja] Go-around | [L] Info landing | [P] Pauza");
        Console.WriteLine(" SYSTEMY:  [O] Silniki | [B] Szyna glowna | [H] Hydraulika | [G] Podwozie awaryjnie | [Z] Autopilot");
        Console.WriteLine(" SKRZYDLA: [C] Spoilery | [1] Klapy dol | [2] Klapy gora | [I] Odladzanie");
        Console.WriteLine(" AWARIE:   [E] Emergency | [R] Napraw anomalie | [X] Gasnice | [K] Uszczelnij wyciek | [V] Widok | [Esc] Wyjscie");
        Console.WriteLine("----------------------------------------------------------------------------------------------------");
        Console.ResetColor();
    }
}
