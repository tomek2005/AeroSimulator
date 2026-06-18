namespace AeroSimulator.Infrastructure;

// Reprezentuje mniejszy, reużywalny komponent graficzny (np. AltimeterWidget, SystemsPanelWidget).
public interface IWidget
{
    void Render();
}