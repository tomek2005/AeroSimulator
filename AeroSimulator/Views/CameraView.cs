using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;
using AeroSimulator.Views.Components;

namespace AeroSimulator.Views;

public class CameraView : IScreen
{
    public string Title => "CAMERA / HORIZON VIEW";

    private readonly Aircraft _aircraft;
    private readonly IWidget _horizonWidget;

    public CameraView(Aircraft aircraft)
    {
        _aircraft = aircraft;
        _horizonWidget = new HorizonWidget(aircraft);
    }

    public void RenderHeader()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("================================================================================");
        Console.WriteLine($" {Title.PadRight(30)} | MODE: {_aircraft.CurrentState.StateName.PadRight(15)} | PRESS 'V' TO PFD VIEW ");
        Console.WriteLine("================================================================================");
        Console.ResetColor();
    }

    public void RenderMainContent()
    {
        var fd = _aircraft.FlightData;
        // USUNIĘTO \n żeby konsola nie skakała!
        Console.WriteLine($" ALT: {fd.Altitude,5:F0} FT  |  SPD: {fd.Speed,3:F0} KTS  |  PITCH: {fd.PitchAngleDeg,4:F1}°  |  ROLL: {fd.RollAngleDeg,4:F1}°");
        
        _horizonWidget.Render();
    }

    public void RenderFooter()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" CONTROLS: [W/S] Pitch  |  [A/D] Roll  |  [UP/DOWN] Throttle | [O] Engines");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.ResetColor();
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
