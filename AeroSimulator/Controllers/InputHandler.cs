using System;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Commands;

namespace AeroSimulator.Controllers;

public class InputHandler
{
    private readonly FlightController _controller;
    private readonly Aircraft _aircraft;
    private readonly AnomalyEngine _anomalyEngine;
    private readonly CommandHistory _history = new();

    public InputHandler(FlightController controller, Aircraft aircraft, AnomalyEngine anomalyEngine)
    {
        _controller = controller;
        _aircraft = aircraft;
        _anomalyEngine = anomalyEngine;
    }

    public void ProcessInput()
    {
        while (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            var key = keyInfo.Key;

            switch (key)
            {
                case ConsoleKey.V: _controller.ToggleView(); break;
                case ConsoleKey.Escape: _controller.Quit(); break;

                // --- STEROWANIE FIZYCZNE (Dostosowane do Maca) ---
                case ConsoleKey.UpArrow: Execute(new SetThrottleCommand(0.1)); break;
                case ConsoleKey.DownArrow: Execute(new SetThrottleCommand(-0.1)); break;
                case ConsoleKey.R: Execute(new ResolveAnomalyCommand(_anomalyEngine)); break;
                case ConsoleKey.F: Execute(new SetThrottleCommand(-0.1)); break;
                case ConsoleKey.W: Execute(new SetPitchCommand(-2.0)); break;
                case ConsoleKey.S: Execute(new SetPitchCommand(2.0)); break;
                case ConsoleKey.A: Execute(new SetHeadingCommand(-5.0)); break;
                case ConsoleKey.D: Execute(new SetHeadingCommand(5.0)); break;
                case ConsoleKey.U: _history.UndoLast(_aircraft); break;

                // --- STANY ---
                case ConsoleKey.T: Execute(new ActivateSystemCommand("TakeOff", "Advance flight phase", a => a.CurrentState.TakeOff(a))); break;
                case ConsoleKey.L: Execute(new ActivateSystemCommand("Land", "Begin landing", a => a.CurrentState.Land(a))); break;
                case ConsoleKey.E: Execute(new EmergencyDeclareCommand()); break;
                case ConsoleKey.Spacebar: Execute(new GoAroundCommand()); break;

                // --- SYSTEMY ---
                case ConsoleKey.O:
                    Execute(new ActivateSystemCommand("Engines", "Toggle all engines", a =>
                    {
                        if (a.EngineSystem.IsOffline) a.EngineSystem.Reboot();
                        else a.EngineSystem.SetOffline();
                    }));
                    break;
                case ConsoleKey.B:
                    Execute(new ActivateSystemCommand("ElectricalBus", "Toggle main bus", a =>
                    {
                        if (a.ElectricalSystem.MainBusVoltage > 0) a.ElectricalSystem.CutMainBus();
                        else a.ElectricalSystem.Reboot();
                    }));
                    break;
                case ConsoleKey.I: Execute(new ActivateSystemCommand("DeIcing", "Activate de-icing", a => a.ElectricalSystem.ActivateDeIcing())); break;
                case ConsoleKey.H:
                    Execute(new ActivateSystemCommand("Hydraulics", "Toggle hydraulics", a =>
                    {
                        if (a.HydraulicSystem.Pressure > 0) a.HydraulicSystem.Depressurize();
                        else a.HydraulicSystem.Reboot();
                    }));
                    break;
                case ConsoleKey.G: Execute(new ActivateSystemCommand("Gear", "Emergency gear extension", a => a.HydraulicSystem.EmergencyGearExtension())); break;
                
                // Uszczelnienie paliwa pod 'K'
                case ConsoleKey.K: Execute(new ActivateSystemCommand("FuelLeak", "Seal fuel leak", a => a.FuelSystem.SealLeak())); break;

                // SKRZYDŁA (Zwykłe cyfry na klawiaturze)
                case ConsoleKey.C: Execute(new ActivateSystemCommand("Spoilers", "Toggle spoilers", a => a.WingSystem.ToggleSpoilers())); break;
                case ConsoleKey.D1: Execute(new ActivateSystemCommand("Flaps", "Flaps down", a => a.WingSystem.SetFlaps(a.WingSystem.FlapsPosition + 0.25))); break;
                case ConsoleKey.D2: Execute(new ActivateSystemCommand("Flaps", "Flaps up", a => a.WingSystem.SetFlaps(a.WingSystem.FlapsPosition - 0.25))); break;

                case ConsoleKey.Z: Execute(new ToggleAutopilotCommand()); break;

                case ConsoleKey.X:
                    Execute(new ActivateSystemCommand("FireSuppression", "Extinguish engine fires", a =>
                    {
                        for (int i = 0; i < a.EngineSystem.EngineCount; i++)
                        {
                            var engine = a.EngineSystem.GetEngine(i);
                            if (engine.IsOnFire) engine.ExtinguishFire();
                        }
                    }));
                    break;
            }
        }
    }

    private void Execute(IFlightCommand command)
    {
        _history.Execute(command, _aircraft);
    }
}
