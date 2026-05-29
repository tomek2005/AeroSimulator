namespace AeroSimulator.Core.Aircraft.Enums;


/// <summary>
/// Identifies which physical location on the aircraft is on fire.
/// Engines are identified by <see cref="FireLocation.EngineIndex"/> rather than
/// by name, so the same type works for any number of engines.
/// </summary>
public class FireLocation
{
    /// <summary>
    /// Zero-based engine index when the fire is on an engine.
    /// <c>-1</c> when the fire is not on an engine (use <see cref="IsWing"/>).
    /// </summary>
    public int EngineIndex { get; }

    /// <summary><see langword="true"/> when this location refers to the wing structure.</summary>
    public bool IsWing => EngineIndex < 0;

    /// <summary><see langword="true"/> when this location refers to a specific engine.</summary>
    public bool IsEngine => EngineIndex >= 0;

    private FireLocation(int engineIndex) => EngineIndex = engineIndex;

    /// <summary>Creates a <see cref="FireLocation"/> for the wing.</summary>
    public static FireLocation Wing { get; } = new(-1);

    /// <summary>
    /// Creates a <see cref="FireLocation"/> for the engine at
    /// <paramref name="index"/> (zero-based).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When index is negative.</exception>
    public static FireLocation Engine(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Engine index must be ≥ 0.");
        return new FireLocation(index);
    }

    /// <inheritdoc/>
    public override string ToString() => IsWing ? "Wing" : $"Engine[{EngineIndex}]";
}
