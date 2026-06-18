using Xunit;
using FluentAssertions;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Tests.Core.Aircraft.Systems;

public class FuelSystemTests
{
    [Fact]
    public void FuelSystem_CheckIgnitionRisk_ZwracaTruePrzyWielkimWycieku()
    {
        var fuelSystem = new FuelSystem();
        
        fuelSystem.StartLeak(160.0);

        fuelSystem.IsLeaking.Should().BeTrue();
        fuelSystem.CheckIgnitionRisk().Should().BeTrue();
    }

    [Fact]
    public void FuelSystem_CheckIgnitionRisk_ZwracaFalsePrzyMalymWycieku()
    {
        var fuelSystem = new FuelSystem();
        
        fuelSystem.StartLeak(50.0);

        fuelSystem.CheckIgnitionRisk().Should().BeFalse();
    }

    [Fact]
    public void FuelSystem_SealLeak_SkutecznieZatrzymujeWyciek()
    {
        var fuelSystem = new FuelSystem();
        fuelSystem.StartLeak(100.0);
        
        bool result = fuelSystem.SealLeak();
        
        result.Should().BeTrue();
        fuelSystem.IsLeaking.Should().BeFalse();
        fuelSystem.CurrentLeakRate.Should().Be(0.0);
    }
}