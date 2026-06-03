namespace AeroSimulator.Core.Aircraft.Systems;

public class DamageModel
{

    public bool IsGameOver { get; set; }
    public string GameOverReason { get; set; } = string.Empty;

    public void TriggerGameOver(string reason)
    {
        IsGameOver = true;
        GameOverReason = reason;
    }

    /// <summary>
    /// Sprawdza, czy gra się skończyła. Mapuje wywołanie na Twoją flagę IsGameOver.
    /// </summary>
    public bool CheckGameOver()
    {
        return IsGameOver;
    }

    public bool AsymmetricDragActive { get; set; } = false;
    public double DriftDegPerSec { get; set; } = 0.0;
}