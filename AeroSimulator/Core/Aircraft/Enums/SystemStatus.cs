namespace AeroSimulator.Core.Aircraft.Enums;

/// <summary>Operational status of an avionics system.</summary>
public enum SystemStatus
{
    /// <summary>System is fully functional.</summary>
    OK,

    /// <summary>System is operational but with reduced performance.</summary>
    Degraded,

    /// <summary>System has failed completely.</summary>
    Failed
}