using System.Collections.Generic;
using Xunit;
using AeroSimulator.Core.Events;
using AeroSimulator.Core.Events.Handlers;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Tests.Core.Events.Handlers;

// Testy, które sprawdzają kolejno czy:
// 1. GenerateReport() poprawnie filtruje i agreguje różne typy zdarzeń z logów za pomocą potoków LINQ.
// 2. GenerateReport() zwraca same zera we wszystkich statystykach, gdy na wejściu otrzyma pustą listę logów.

public class StatisticsHandlerTests
{
    [Fact]
    public void GenerateReport_PoprawnieAgregujeZdarzeniaZaPomocaPotokowLINQ()
    {
        var dummyLogs = new List<FlightEvent>
        {
            new AnomalyTriggeredEvent("ENGINE", Severity.Critical, "Fire detected"), 
            new SystemFailureEvent("FUEL", 0.0, "Critical leak"),   
            new StateChangedEvent("CLIMB", "CRUISE", "Leveling off")
        };

        FlightStatistics report = StatisticsHandler.GenerateReport(dummyLogs);

        Assert.Equal(1, report.TotalAnomalies);     
        Assert.Equal(1, report.TotalFailures);      
        Assert.Equal(1, report.StateTransitions);   
        Assert.Equal(0, report.TotalCascades);      
        Assert.Equal(2, report.CriticalAlerts);     
    }

    [Fact]
    public void GenerateReport_DlaPustegoLoguZwracaZera()
    {
        var emptyLogs = new List<FlightEvent>();

        FlightStatistics report = StatisticsHandler.GenerateReport(emptyLogs);

        Assert.Equal(0, report.TotalAnomalies);
        Assert.Equal(0, report.TotalFailures);
        Assert.Equal(0, report.StateTransitions);
        Assert.Equal(0, report.CriticalAlerts);
        Assert.Equal(0, report.TotalCascades);
    }
}