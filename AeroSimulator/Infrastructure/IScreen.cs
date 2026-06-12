namespace AeroSimulator.Infrastructure;

/// <summary>
/// Reprezentuje pełny ekran gry lub menu (np. StartupScreen, ConsoleDashboardView),
/// wymuszając obecność nagłówka, sekcji głównej oraz legendy klawiszologii.
/// </summary>
public interface IScreen
{
    string Title { get; }
    
    void RenderHeader();
    void RenderMainContent();
    void RenderFooter(); // Legenda, akcje użytkownika
    
    void RenderAll(); // Składa cały ekran razem
    void HandleInput(ConsoleKey key);
}