namespace AeroSimulator.Core.Aircraft.Enums;

/// <summary>Represents the progression of a fire on an aircraft component.</summary>
public enum FireState
{
    /// <summary>No fire present.</summary>
    None,

    /// <summary>Fire has ignited; component health is decaying.</summary>
    Burning,

    /// <summary>Fire is spreading to adjacent components.</summary>
    Spreading,

    /// <summary>Component is melting; structural integrity compromised.</summary>
    Melting
}