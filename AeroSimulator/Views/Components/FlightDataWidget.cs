using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;
using System;

namespace AeroSimulator.Views.Components;

public class FlightDataWidget : IWidget
{
    private readonly Aircraft _aircraft;

    public FlightDataWidget(Aircraft aircraft) => _aircraft = aircraft;

    public void Render()
    {
        var fd = _aircraft.FlightData;
        var sensors = _aircraft.Sensors;
        
        double displayAlt = sensors.GetReading(sensors.Altitude.SensorName).ValueOr(-1.0);
        double displaySpd = sensors.GetReading(sensors.Airspeed.SensorName).ValueOr(-1.0);
        double displayFuel = sensors.GetReading(sensors.FuelLevel.SensorName).ValueOr(-1.0);
        
        double capacity =
            fd.FuelCapacityKg > 0 ? fd.FuelCapacityKg : 10000.0;
        double fuelPct = Math.Clamp(displayFuel / capacity, 0.0, 1.0);
        int filledBars = (int)(fuelPct * 10);
        string fuelBar = new string('█', filledBars).PadRight(10, '░');

        Console.WriteLine("\n[ NAVIGATION & DYNAMICS ]");
        Console.WriteLine(
            $" Flight State: {_aircraft.CurrentState.StateName,-10} | {_aircraft.CurrentState.StateDescription}");
        Console.WriteLine($" Target HDG:  {fd.TargetHeading,7:F0} DEG   | Target ALT:    {fd.TargetAltitude,7:F0} FT");
        Console.WriteLine($" Altitude:    {displayAlt,7:F0} FT    | Vertical Spd:  {fd.VerticalSpeed,7:F0} FT/MIN");
        Console.WriteLine($" Airspeed:    {displaySpd,7:F0} KTS   | Heading (IMU): {fd.Heading,7:F0} DEG");
        Console.WriteLine($" Pitch Angle: {fd.PitchAngleDeg,7:F1} DEG   | Roll Angle:    {fd.RollAngleDeg,7:F1} DEG");
        Console.WriteLine(
            $" Throttle:    {fd.Throttle * 100,7:F0} %     | Fuel: [{fuelBar}] {fuelPct * 100:F1}% ({fd.FuelFlowKgPerH:F0} kg/h)");
    }
}