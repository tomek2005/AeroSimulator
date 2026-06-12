using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class ConsoleDashboardView : IScreen
{
    public string Title => "PRIMARY FLIGHT DISPLAY (PFD)";
    
    private readonly Aircraft _aircraft;
    private readonly IWidget _flightDataWidget;
    private readonly IWidget _systemsWidget;
    private readonly IWidget _legendWidget; 
    private readonly IWidget _alertWidget;

    public ConsoleDashboardView(Aircraft aircraft)
    {
        _aircraft = aircraft;
        _flightDataWidget = new FlightDataWidget(aircraft);
        _systemsWidget = new SystemsPanelWidget(aircraft);
        _legendWidget = new LegendWidget(); 
        _alertWidget = new AlertWidget();
    }

    public void RenderHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine($" {Title.PadRight(30)} | MODE: {_aircraft.CurrentState.StateName.PadRight(15)} | PRESS 'V' TO CAM VIEW ");
        Console.WriteLine("================================================================================");
        Console.ResetColor();
    }

    public void RenderMainContent()
    {
        _flightDataWidget.Render();  
        _systemsWidget.Render();     
        _alertWidget.Render();       
    }

    public void RenderFooter()
    {
        _legendWidget.Render(); 
    }
    
    public void RenderAll()
    {
        ClearViewport();
        Console.SetCursorPosition(0, 0);
        RenderHeader();
        RenderMainContent();
        RenderFooter();
    }

    private static void ClearViewport()
    {
        int width = Math.Max(1, Console.BufferWidth - 1);
        int height = Console.WindowHeight;
        string blank = new(' ', width);

        for (int row = 0; row < height; row++)
        {
            Console.SetCursorPosition(0, row);
            Console.Write(blank);
        }
    }

    public void HandleInput(ConsoleKey key) { }
}
