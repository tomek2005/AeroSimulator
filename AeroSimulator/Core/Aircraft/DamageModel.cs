using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Aircraft.Systems;

/// <summary>
/// Centralny model zniszczeń samolotu. Śledzi stan pożarów silników,
/// zdrowie skrzydła i flagi kończące grę.
/// </summary>
public class DamageModel
{
    private readonly Dictionary<int, FireState> _engineFireStates = new();
    private readonly Dictionary<int, double>    _engineHealths    = new();

    public FireState WingFireState       { get; set; } = FireState.None;
    public double    WingHealth          { get; set; } = 1.0;

    // Właściwość z publicznym setterem — wymagana przez WingFireAnomaly
    public bool   AsymmetricDragActive  { get; set; }
    public string AsymmetricDragSide    { get; private set; } = "NONE";
    public double DriftDegPerSec        { get; private set; }

    public bool   IsExploded            { get; private set; }
    public bool   IsGameOver            { get; private set; }
    public string GameOverReason        { get; private set; } = string.Empty;

    public DamageModel(int totalEngines)
    {
        for (int i = 0; i < totalEngines; i++)
        {
            _engineFireStates[i] = FireState.None;
            _engineHealths[i]    = 1.0;
        }
    }

    // ── API silników ──────────────────────────────────────────────────────────

    public FireState GetEngineFireState(int engineIndex)
        => _engineFireStates.TryGetValue(engineIndex, out var state) ? state : FireState.None;

    public void SetEngineFireState(int engineIndex, FireState state)
    {
        if (_engineFireStates.ContainsKey(engineIndex))
            _engineFireStates[engineIndex] = state;
    }

    public double GetEngineHealth(int engineIndex)
        => _engineHealths.TryGetValue(engineIndex, out var health) ? health : 1.0;

    public void ApplyEngineDamage(int engineIndex, double amount)
    {
        if (_engineHealths.ContainsKey(engineIndex))
            _engineHealths[engineIndex] = Math.Clamp(_engineHealths[engineIndex] - amount, 0.0, 1.0);
    }

    // ── Pętla symulacji ───────────────────────────────────────────────────────

    public void Update(double dt)
    {
        if (IsGameOver) return;

        // Asymetryczny opór przy niskim zdrowiu skrzydła (< 20 %)
        if (WingHealth < 0.20 && WingHealth > 0.0)
        {
            AsymmetricDragActive = true;
            AsymmetricDragSide   = "LEFT";
            DriftDegPerSec       = (1.0 - WingHealth) * 5.0;
        }

        EvaluateStructuralIntegrity();
    }

    private void EvaluateStructuralIntegrity()
    {
        if (WingHealth <= 0.0)
        {
            TriggerGameOver("Wing structural failure — total loss of lift.");
            return;
        }

        if (IsExploded)
            TriggerGameOver("Critical hull breach due to engine explosion.");
    }

    public void TriggerExplosion() => IsExploded = true;

    public void TriggerGameOver(string reason)
    {
        if (IsGameOver) return; // tylko raz
        IsGameOver     = true;
        GameOverReason = reason;
    }
}