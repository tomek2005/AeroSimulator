using System;
using System.Collections.Generic;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Events.Handlers;

public class AlertSystemHandler : IFlightEventHandler
{
    private static readonly List<string> _activeAlerts = new();
    public static IReadOnlyList<string> ActiveAlerts => _activeAlerts;

    public void Handle(FlightEvent evt)
    {
        if (evt.Level == Severity.Medium || evt.Level == Severity.High || evt.Level == Severity.Critical)
        {
            string prefix = evt.Level switch
            {
                Severity.Medium => "[WARNING]",
                Severity.High => "[ALERT]",
                Severity.Critical => "[CRITICAL MAYDAY]",
                _ => "[SYSTEM]"
            };

            string formattedAlert = $"{prefix} {evt.Timestamp:HH:mm:ss} -> {evt.Message}";

            _activeAlerts.Add(formattedAlert);
            
            if (_activeAlerts.Count > 4)
            {
                _activeAlerts.RemoveAt(0);
            }
        }
    }
}