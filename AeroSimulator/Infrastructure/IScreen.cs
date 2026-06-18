namespace AeroSimulator.Infrastructure;

// Reprezentuje pełny ekran gry lub menu (np. StartupScreen, ConsoleDashboardView),
public interface IScreen
{
    string Title { get; }

    void RenderHeader();
    void RenderMainContent();
    void RenderFooter();

    void RenderAll();
    void HandleInput(ConsoleKey key);
}