namespace AeroSimulator.Core.Aircraft.Enums;


// Identifies which physical location on the aircraft is on fire.
public class FireLocation
{
    public int EngineIndex { get; }
    
    public bool IsWing => EngineIndex < 0;
    
    public bool IsEngine => EngineIndex >= 0;

    private FireLocation(int engineIndex) => EngineIndex = engineIndex;
    
    public static FireLocation Wing { get; } = new(-1);
    
    public static FireLocation Engine(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Engine index must be ≥ 0.");
        return new FireLocation(index);
    }
    
    public override string ToString() => IsWing ? "Wing" : $"Engine[{EngineIndex}]";
}