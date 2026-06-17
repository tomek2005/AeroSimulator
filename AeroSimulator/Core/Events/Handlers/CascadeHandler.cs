using System;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events.Handlers;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

public class CascadeHandler : IFlightEventHandler
{
    private readonly Aircraft _aircraft;
    private readonly Random _rng = new();

    public CascadeHandler(Aircraft aircraft)
    {
        _aircraft = aircraft ?? throw new ArgumentNullException(nameof(aircraft));
    }

    public void Handle(FlightEvent evt)
    {
        if (evt is AnomalyTriggeredEvent anomaly)
        {
            switch (NormalizeName(anomaly.AnomalyName))
            {
                // ŁAŃCUCH 1: Ptak -> Pożar silnika -> Pożar Skrzydła -> Znoszenie -> Game Over
                case "BIRD_STRIKE":
                    if (_rng.NextDouble() < 0.40) 
                    {
                        int engineIndex = _rng.Next(0, _aircraft.EngineCount);

                        _aircraft.Publish(new CascadeTriggeredEvent("BirdStrike", "EngineFire",
                            $"Bird strike ingested into Engine {engineIndex + 1}!"));
                        _aircraft.Publish(new EngineFireEvent(engineIndex,
                            $"Catastrophic Fire detected in Engine {engineIndex + 1}!"));
                        
                        _aircraft.DamageModel.SetEngineFireState(engineIndex, FireState.Burning);
                        _aircraft.EngineSystem.GetEngine(engineIndex).StartFire();
                    }

                    break;

                // ŁAŃCUCH 2: Silne Turbulencje -> Uszkodzenie Czujnika -> Awaria Autopilota
                case "TURBULENCE":
                case "SEVERE_TURBULENCE":
                    if (_rng.NextDouble() < 0.35)
                    {
                        _aircraft.Publish(new CascadeTriggeredEvent("SevereTurbulence", "SensorFault",
                            "Violent shaking damaged external sensors!"));
                        _aircraft.Sensors.DamageRandomSensor();

                        if (_aircraft.AutopilotSystem.IsEngaged)
                        {
                            _aircraft.Publish(new SystemFailureEvent("Autopilot", 0.0,
                                "Autopilot disconnected due to unreliable sensor data!"));
                            _aircraft.AutopilotSystem.SetOffline();
                        }
                    }

                    break;

                // ŁAŃCUCH 3: Wyciek Hydrauliki -> Zacięcie Podwozia
                case "HYDRAULIC_FAILURE":
                case "HYDRAULIC_LEAK":
                    if (_rng.NextDouble() < 0.50)
                    {
                        _aircraft.Publish(new CascadeTriggeredEvent("HydraulicLeak", "GearJammed",
                            "Loss of pressure jammed the landing gear mechanism."));
                        _aircraft.HydraulicSystem.JamGear();
                    }

                    break;
            }
        }
        else if (evt is SystemFailureEvent failure && TryHandleCascadeToken(failure.Message))
        {
            return;
        }
        else if (evt is EngineFireEvent fireEvent)
        {
            if (_rng.NextDouble() < 0.30)
            {
                _aircraft.Publish(new CascadeTriggeredEvent("EngineFire", "WingDamage",
                    "Engine fire spreading and damaging the wing structure!"));
                _aircraft.Publish(new WingFireEvent("LEFT", "Left wing structural integrity decreasing due to fire!"));
                
                _aircraft.DamageModel.WingFireState = FireState.Spreading;
                
                _aircraft.DamageModel.WingHealth = Math.Max(0.0, _aircraft.DamageModel.WingHealth - 0.85);
            }
            
            _aircraft.DamageModel.ApplyEngineDamage(fireEvent.EngineNumber, 0.50);
            
            if (_aircraft.DamageModel.GetEngineHealth(fireEvent.EngineNumber) <= 0)
            {
                _aircraft.Publish(new EngineExplosionEvent(fireEvent.EngineNumber,
                    "Engine completely destroyed and exploded!"));
                
                _aircraft.DamageModel.TriggerExplosion();
            }
        }
    }

    private bool TryHandleCascadeToken(string message)
    {
        const string prefix = "CASCADE:";
        int start = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return false;

        string token = message[(start + prefix.Length)..];
        string[] parts = token.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        string type = parts[0].ToUpperInvariant();
        int componentIndex = 0;
        if (parts.Length > 1 && int.TryParse(parts[1], out int parsed))
        {
            componentIndex = Math.Clamp(parsed, 0, _aircraft.EngineCount - 1);
        }

        switch (type)
        {
            case "ENGINE_FIRE":
                _aircraft.Publish(new CascadeTriggeredEvent("SystemFailure", "EngineFire",
                    $"Cascade started engine {componentIndex + 1} fire."));
                _aircraft.DamageModel.SetEngineFireState(componentIndex, FireState.Burning);
                _aircraft.GetEngine(componentIndex).StartFire();
                _aircraft.Publish(new EngineFireEvent(componentIndex,
                    $"Engine {componentIndex + 1} fire started by cascade."));
                return true;

            case "WING_FIRE":
                _aircraft.Publish(new CascadeTriggeredEvent("EngineFire", "WingFire",
                    "Engine fire spread to wing structure."));
                _aircraft.DamageModel.WingFireState = FireState.Spreading;
                _aircraft.DamageModel.WingHealth = Math.Max(0.0, _aircraft.DamageModel.WingHealth - 0.35);
                _aircraft.WingSystem.SetOffline();
                _aircraft.Publish(new WingFireEvent(componentIndex % 2 == 0 ? "LEFT" : "RIGHT",
                    "Wing structural fire triggered by cascade."));
                return true;
        }

        return false;
    }

    private static string NormalizeName(string name)
    {
        return name.Trim()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .ToUpperInvariant();
    }
}