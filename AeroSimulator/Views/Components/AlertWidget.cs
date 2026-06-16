using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Events.Handlers;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class AlertWidget : IWidget
{
    public void Render()
    {
        Console.WriteLine("\n[ SYSTEM ALERTS & EVENT LOGS ]");
        
        var logs = AlertBufferHandler.RecentLogs;
        
        if (logs.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" > Systems stable. Monitoring EventBus...");
            Console.WriteLine(" >");
            Console.WriteLine(" >");
        }
        else
        {
            foreach (var log in logs)
            {
                // Kolorowanie linii w zależności od stopnia powagi sytuacji
                if (log.Contains("[ALERT]") || log.Contains("CRITICAL") || log.Contains("FIRE"))
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (log.Contains("[SUCCESS]") || log.Contains("REPAIR COMPLETE"))
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (log.Contains("[WARN]"))
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    Console.ForegroundColor = ConsoleColor.Cyan;

                Console.WriteLine($" > {log}");
            }
            
            // Dopełnienie ramki, żeby interfejs w konsoli nie skakał góra/dół
            for (int i = 0; i < 3 - logs.Count; i++) Console.WriteLine(" >");
        }
        Console.ResetColor();
    }
}
