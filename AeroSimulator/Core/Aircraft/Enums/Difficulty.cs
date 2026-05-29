namespace AeroSimulator.Core.Aircraft.Enums;

/// <summary>Difficulty setting that scales anomaly frequency and cascade probability.</summary>
public enum Difficulty
{
    /// <summary>No anomalies are spawned.</summary>
    Easy,

    /// <summary>Normal anomaly frequency with cascades enabled.</summary>
    Normal,

    /// <summary>Increased frequency with full cascade chains.</summary>
    Hard
}