namespace AeroSimulator.Infrastructure;

/// <summary>
/// Reprezentuje mniejszy, reużywalny komponent graficzny (np. AltimeterWidget, SystemsPanelWidget).
/// </summary>
public interface IWidget
{
    void Render();
}