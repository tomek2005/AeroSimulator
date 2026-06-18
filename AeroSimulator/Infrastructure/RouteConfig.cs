namespace AeroSimulator.Infrastructure;

// Reprezentuje trasę przelotu wybraną przez użytkownika.
public record RouteConfig(
    string Name,
    string Description,
    double DistanceKm,
    string EstimatedTime
);