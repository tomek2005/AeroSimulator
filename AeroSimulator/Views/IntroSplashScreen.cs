using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views;

public class IntroSplashScreen : IScreen
{
    public string Title => "AeroSim - Splash Screen";

    public async Task ShowAsync()
    {
        Console.CursorVisible = false;
        RenderAll();
        
        var timeoutTask = Task.Delay(3000);
        var inputTask = Task.Run(() => Console.ReadKey(true));

        var completedTask = await Task.WhenAny(timeoutTask, inputTask);

        if (completedTask == inputTask)
        {
            while (Console.KeyAvailable) Console.ReadKey(true);
        }
    }

    public void RenderAll()
    {
        Console.Clear();
        RenderHeader();
        RenderMainContent();
        RenderFooter();
    }

    public void RenderHeader()
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(@"  [AEROSIMULATION - INICJALIZACJA]");
        Console.ResetColor();
    }

    public void RenderMainContent()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n\n");
        Console.WriteLine(@"               ______                                             ");
        Console.WriteLine(@"               _\ _~-\___                                         ");
        Console.WriteLine(@"       =  = ==(____AA____D                                        ");
        Console.WriteLine(@"                   \_____\___________________,-~~~~~~~`-.._       ");
        Console.WriteLine(@"                   /     o O o o o o O O o o o o o o O o  |\_     ");
        Console.WriteLine(@"                   `~-.__        ___..----..                  )   ");
        Console.WriteLine(@"                         `---~~\___________/------------`````     ");
        Console.WriteLine(@"                         =  ===(_________D                        ");
        Console.WriteLine("\n");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("                A E R O S I M   F L I G H T   E N G I N E         ");
        Console.WriteLine("             =================================================    ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("                      Wczytywanie modułów systemu...              ");
        Console.ResetColor();
    }

    public void RenderFooter()
    {
        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Naciśnij dowolny klawisz, aby pominąć...");
        Console.ResetColor();
    }

    public void HandleInput(ConsoleKey key)
    {
    }
}