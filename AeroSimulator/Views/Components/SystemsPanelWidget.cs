using AeroSimulator.Core.Aircraft;
using AeroSimulator.Infrastructure;
using System;

namespace AeroSimulator.Views.Components;

public class SystemsPanelWidget : IWidget
{
    private readonly Aircraft _aircraft;

    public SystemsPanelWidget(Aircraft aircraft) => _aircraft = aircraft;

    public void Render()
    {
        var sensors = _aircraft.Sensors;
        var elec = _aircraft.ElectricalSystem;
        var hydr = _aircraft.HydraulicSystem;
        var wing = _aircraft.WingSystem;
        var fuel = _aircraft.FuelSystem;
        var ap = _aircraft.AutopilotSystem;
        var engSys = _aircraft.EngineSystem;
        var weather = _aircraft.WeatherSystem;
        
        // --- BEZPIECZNE ROZPAKOWANIE MONADY (PALIWO) ---
        var fuelReading = sensors.GetReading(sensors.FuelLevel.SensorName);
        double displayFuel = fuelReading.HasValue ? fuelReading.Value : -1.0;

        Console.WriteLine("\n[ SYSTEMS & POWER ]");
        
        // --- PALIWO I ELEKTRYKA ---
        string fuelStatus = fuel.IsLeaking ? "LEAKING [!]" : "OK";
        Console.WriteLine($" Fuel: {displayFuel,6:F0} KG ({fuelStatus}) | Elec Main: {elec.MainBusVoltage,4:F1}V | Sec: {elec.SecondaryBusVoltage,4:F1}V | De-Ice: {(elec.IsDeIcingActive ? "ON" : "OFF")}");
        
        // --- HYDRAULIKA I SKRZYDŁA ---
        string gearStatus = hydr.GearJammed ? "JAMMED!" : (hydr.IsGearTransiting ? "TRANSITING" : "STABLE");
        Console.WriteLine($" Hydr Press: {hydr.Pressure,6:F0} PSI  | Gear: {gearStatus,-10} | Flaps: {wing.FlapsPosition * 100,3:F0}% | Spoilers: {(wing.SpoilersDeployed ? "UP" : "DOWN")}");

        // --- AUTOPILOT I OBLODZENIE ---
        string apStatus = ap.IsOffline ? "OFFLINE" : (ap.IsEngaged ? "ENGAGED" : "STDBY");
        Console.WriteLine($" Autopilot:  {apStatus,-10} | WX Radar: {(weather.IsOffline ? "OFF" : "ON "),-3} | Wind: {_aircraft.FlightData.WindDirectionDeg,3:F0}/{_aircraft.FlightData.WindSpeedKnots,2:F0} kt | Ice: {wing.IceAccumulation * 100,3:F0}% {(wing.IsIceCritical() ? "[CRITICAL!]" : "")}");

        // --- SILNIKI (Sensory + Status Pożaru) ---
        Console.Write(" Engines: ");
        for(int i = 0; i < engSys.EngineCount; i++)
        {
            // --- BEZPIECZNE ROZPAKOWANIE MONADY (OBROTY SILNIKA) ---
            var rpmReading = sensors.GetReading(sensors.EngineRPMs[i].SensorName);
            double rpm = rpmReading.HasValue ? rpmReading.Value : -1.0;

            var engine = engSys.GetEngine(i);
            bool isOnFire = engine.IsOnFire;
            
            if (isOnFire) Console.ForegroundColor = ConsoleColor.Red;
            else if (!engine.IsRunning) Console.ForegroundColor = ConsoleColor.DarkGray;
            else if (engine.Health < 0.5) Console.ForegroundColor = ConsoleColor.Yellow;
            else Console.ForegroundColor = ConsoleColor.Green;

            string status = isOnFire ? "FIRE" : engine.IsRunning ? "OK" : "OFF";
            Console.Write($"[E{i+1}: {rpm,4:F0} RPM {status}] ");
            Console.ResetColor();
        }
        Console.WriteLine();
    }
}