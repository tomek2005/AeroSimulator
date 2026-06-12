using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class MenuHeaderWidget : IWidget
{
    private readonly string _title;

    public MenuHeaderWidget(string title)
    {
        _title = title;
    }

    public void Render()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("====================================================================");
        Console.WriteLine($"  {_title.ToUpper()}");
        Console.WriteLine("====================================================================");
        Console.ResetColor();
        Console.WriteLine();
    }
}