using AeroSimulator.Infrastructure;

namespace AeroSimulator.Views.Components;

public class SettingsSummaryWidget : IWidget
{
    private readonly Func<SimulationConfig> _configProvider;

    public SettingsSummaryWidget(Func<SimulationConfig> configProvider)
    {
        _configProvider = configProvider;
    }

    public void Render()
    {
        var config = _configProvider();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  [ PODGLĄD PARAMETRÓW POZIOMU TRUDNOŚCI ]");
        Console.ResetColor();
        Console.WriteLine($"  Modyfikator szansy kaskad:      {config.CascadeProbabilityMultiplier:F1}x");
        Console.WriteLine($"  Częstotliwość anomalii / tick:  {config.AnomalyChancePerTick:P2}");
        Console.WriteLine($"  Maksymalna prędkość wiatru:     {config.MaxWindSpeedKnots} węzłów (kts)");
        Console.WriteLine($"  Usterka czujnika w turbulencji: {config.TurbulenceSensorFaultChance:P0}");
        Console.WriteLine();
    }
}