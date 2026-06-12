namespace AeroSimulator.Infrastructure;

/// <summary>
/// Reprezentuje trasę przelotu wybraną przez użytkownika.
/// </summary>
public record RouteConfig(
    string Name, 
    string Description, 
    double DistanceKm, 
    string EstimatedTime
);