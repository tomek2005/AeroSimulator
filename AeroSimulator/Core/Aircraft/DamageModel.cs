using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Aircraft.Systems;

/// <summary>
/// KORPORACYJNY MODEL SYSTEMU ZNISZCZEŃ (Komponent warstwy MODEL w MVC).
/// Zaprojektowany obiektowo — w pełni niezależny od liczby silników statku powietrznego.
/// </summary>
public class DamageModel
{
    // Słownik mapujący Indeks Silnika (0, 1, 2, 3...) na jego aktualny stan pożaru
    private readonly Dictionary<int, FireState> _engineFireStates = new();
    
    // Słownik przechowujący integralność strukturalną poszczególnych silników (0.0 do 1.0)
    private readonly Dictionary<int, double> _engineHealths = new();

    public FireState WingFireState { get; set; } = FireState.None;
    public double WingHealth { get; set; } = 1.0; 
    
    // Stan aerodynamiczny wywołany zniszczeniami
    public bool AsymmetricDragActive { get; private set; }
    public string AsymmetricDragSide { get; private set; } = "NONE";
    public double DriftDegPerSec { get; private set; }

    // Flagi stanu symulacji
    public bool IsExploded { get; private set; }
    public bool IsGameOver { get; private set; }
    public string GameOverReason { get; private set; } = string.Empty;

    /// <summary>
    /// Inicjalizacja modelu zniszczeń na podstawie faktycznych danych technicznych samolotu.
    /// </summary>
    public DamageModel(int totalEngines)
    {
        for (int i = 0; i < totalEngines; i++)
        {
            _engineFireStates[i] = FireState.None;
            _engineHealths[i] = 1.0;
        }
    }

    // --- KORPORACYJNE API: Bezpieczny dostęp per komponent ---

    public FireState GetEngineFireState(int engineIndex)
    {
        return _engineFireStates.TryGetValue(engineIndex, out var state) ? state : FireState.None;
    }

    public void SetEngineFireState(int engineIndex, FireState state)
    {
        if (_engineFireStates.ContainsKey(engineIndex))
        {
            _engineFireStates[engineIndex] = state;
        }
    }

    public double GetEngineHealth(int engineIndex)
    {
        return _engineHealths.TryGetValue(engineIndex, out var health) ? health : 1.0;
    }

    public void ApplyEngineDamage(int engineIndex, double amount)
    {
        if (_engineHealths.ContainsKey(engineIndex))
        {
            _engineHealths[engineIndex] = Math.Clamp(_engineHealths[engineIndex] - amount, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Przetwarzanie kaskad fizycznych i strukturalnych w pętli symulacji.
    /// </summary>
    public void Update(double dt)
    {
        if (IsGameOver) return;

        // 1. Obliczanie asymetrii skrzydeł (Zasada z sekcji 3.1)
        if (WingHealth < 0.20 && WingHealth > 0.0)
        {
            AsymmetricDragActive = true;
            AsymmetricDragSide = "LEFT"; // Można rozbudować o analizę, które skrzydło ucierpiało
            DriftDegPerSec = (1.0 - WingHealth) * 5.0; 
        }

        // 2. Sprawdzanie strukturalnych warunków Game Over
        EvaluateStructuralIntegrity();
    }

    /// <summary>
    /// Centralna weryfikacja czy uszkodzenia doprowadziły do katastrofy.
    /// </summary>
    private void EvaluateStructuralIntegrity()
    {
        if (WingHealth <= 0.0)
        {
            TriggerGameOver("Wing structural failure — total loss of lift.");
            return;
        }

        // Przykład korporacyjny: Jeśli wszystkie silniki mają health == 0, to nie jest jeszcze game over (szybowanie),
        // ale jeśli eksplodują (IsExploded) - natychmiastowy koniec.
        if (IsExploded)
        {
            TriggerGameOver("Critical hull breach due to engine explosion.");
        }
    }

    public void TriggerExplosion()
    {
        IsExploded = true;
    }

    public void TriggerGameOver(string reason)
    {
        IsGameOver = true;
        GameOverReason = reason;
    }
}
