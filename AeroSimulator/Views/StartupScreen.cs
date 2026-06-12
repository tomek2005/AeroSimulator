using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;
using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class StartupScreen : IScreen
{
    public string Title => "AeroSim – Panel Konfiguracji Symulatora";

    private readonly IReadOnlyList<AircraftConfig> _availableAircrafts;
    private readonly IReadOnlyList<RouteConfig> _availableRoutes;

    private int _currentSelection = 0; 
    private int _aircraftIndex = 0;
    private int _routeIndex = 0;
    private Difficulty _difficulty = Difficulty.Normal;
    private bool _realTime = true;
    
    private bool _menuLoopActive = true;

    private readonly MenuHeaderWidget _headerWidget;
    private readonly SettingsSummaryWidget _summaryWidget;

    // ZMIANA: Mamy teraz TYLKO JEDNĄ właściwość wyjściową dla całej aplikacji
    public SimulationConfig FinalConfig { get; private set; }
    
    public bool IsConfigurationFinished { get; private set; }

    public StartupScreen(IReadOnlyList<AircraftConfig> aircrafts, IReadOnlyList<RouteConfig> routes)
    {
        _availableAircrafts = aircrafts ?? throw new ArgumentNullException(nameof(aircrafts));
        _availableRoutes = routes ?? throw new ArgumentNullException(nameof(routes));
        
        // ZMIANA: Inicjalizacja domyślnego SimulationConfig przy starcie ekranu
        FinalConfig = new SimulationConfig(
            _difficulty, 
            _availableAircrafts[0], 
            _availableRoutes[0], 
            RealTime: _realTime
        );

        _headerWidget = new MenuHeaderWidget(Title);
        // ZMIANA: Przekazujemy FinalConfig do widżetu
        _summaryWidget = new SettingsSummaryWidget(() => FinalConfig); 
    }

    public async Task RunScreenLifecycleAsync()
    {
        Console.CursorVisible = false;
        RenderAll();

        while (_menuLoopActive)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = await Task.Run(() => Console.ReadKey(true));
                HandleInput(keyInfo.Key);
                
                if (_menuLoopActive)
                {
                    RenderAll();
                }
            }
            await Task.Delay(30);
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
        _headerWidget.Render();
    }

    public void RenderMainContent()
    {
        var aircraftStr = $"{_availableAircrafts[_aircraftIndex].DisplayName} ({_availableAircrafts[_aircraftIndex].TailNumber})";
        var routeStr = $"{_availableRoutes[_routeIndex].Name} - {_availableRoutes[_routeIndex].DistanceKm}km";

        RenderOptionLine("1. Wybór Typu Samolotu:", aircraftStr, _currentSelection == 0);
        RenderOptionLine("2. Planowana Trasa:", routeStr, _currentSelection == 1);
        RenderOptionLine("3. Poziom Trudności Gry:", _difficulty.ToString().ToUpper(), _currentSelection == 2);
        RenderOptionLine("4. Tryb Czasu Rzeczywistego:", _realTime ? "WŁĄCZONY" : "WYŁĄCZONY", _currentSelection == 3);

        Console.WriteLine();
        if (_currentSelection == 4)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" >>>> [ POTWIERDŹ I PRZEJDŹ DO SYMULATORA (ENTER) ] <<<<");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("      [ POTWIERDŹ I PRZEJDŹ DO SYMULATORA ]");
        }

        Console.WriteLine("\n" + new string('-', 68));
        _summaryWidget.Render();    
    }

    public void RenderFooter()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('=', 68));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" LEGENDA NAWIGACJI:");
        Console.WriteLine(" [↑ / ↓]   – Nawigacja po menu (Opcje 1-4 oraz przycisk Start)");
        Console.WriteLine(" [← / →]   – Zmiana parametrów wybranej opcji");
        Console.WriteLine(" [ENTER]   – Zatwierdzenie wyboru");
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" ℹ️ Dolny panel parametrów aktualizuje się automatycznie dla wybranej trudności.");
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('=', 68));
        Console.ResetColor();
    }

    public void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
                _currentSelection = (_currentSelection - 1 + 5) % 5;
                break;
            case ConsoleKey.DownArrow:
                _currentSelection = (_currentSelection + 1) % 5;
                break;
            case ConsoleKey.LeftArrow:
                ModifyValue(-1);
                break;
            case ConsoleKey.RightArrow:
                ModifyValue(1);
                break;
            case ConsoleKey.Enter:
                if (_currentSelection == 4)
                {
                    _menuLoopActive = false;
                    IsConfigurationFinished = true; 
                }
                break;
        }

        // ZMIANA: Tworzymy tutaj świeży, niezmienny obiekt SimulationConfig z nowymi wartościami
        FinalConfig = new SimulationConfig(
            Difficulty: _difficulty,
            Aircraft: _availableAircrafts[_aircraftIndex],
            Route: _availableRoutes[_routeIndex],
            RealTime: _realTime
            // Zauważ: TimeStepDeltaT i LogFilePath biorą się same z domyślnych wartości rekordu!
        );
    }

    private void ModifyValue(int dir)
    {
        if (_currentSelection == 0)
            _aircraftIndex = (_aircraftIndex + dir + _availableAircrafts.Count) % _availableAircrafts.Count;
        else if (_currentSelection == 1)
            _routeIndex = (_routeIndex + dir + _availableRoutes.Count) % _availableRoutes.Count;
        else if (_currentSelection == 2)
        {
            int diff = (int)_difficulty;
            diff = (diff + dir + 3) % 3;
            _difficulty = (Difficulty)diff;
        }
        else if (_currentSelection == 3)
            _realTime = !_realTime;
    }

    private void RenderOptionLine(string label, string value, bool isSelected)
    {
        if (isSelected)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine($" > {label,-30} [ {value} ] < ");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"   {label,-30}   {value}   ");
        }
    }
}