using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class HorizonWidget : IWidget
{
    private readonly Aircraft _aircraft;

    public HorizonWidget(Aircraft aircraft) => _aircraft = aircraft;

    public void Render()
    {
        var roll = _aircraft.FlightData.RollAngleDeg;
        
        Console.WriteLine();
        if (roll < -15.0)
        {
            Console.WriteLine(@"           \             ");
            Console.WriteLine(@"            \            ");
            Console.WriteLine(@"          --=O=--        ");
            Console.WriteLine(@"              \          ");
            Console.WriteLine(@" BANKING LEFT  \         ");
        }
        else if (roll > 15.0)
        {
            Console.WriteLine(@"             /           ");
            Console.WriteLine(@"            /            ");
            Console.WriteLine(@"          --=O=--        ");
            Console.WriteLine(@"           /             ");
            Console.WriteLine(@"          / BANKING RIGHT");
        }
        else
        {
            Console.WriteLine(@"                         ");
            Console.WriteLine(@"                         ");
            Console.WriteLine(@"        ---==O==---      ");
            Console.WriteLine(@"                         ");
            Console.WriteLine(@"       LEVEL FLIGHT      ");
        }
        Console.WriteLine();
    }
}