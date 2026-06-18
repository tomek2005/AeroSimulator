namespace AeroSimulator.Core.Strategies.Weather;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public interface IWeatherStrategy
{
    string Name { get; }
    void Apply(Aircraft aircraft, double dt);
}