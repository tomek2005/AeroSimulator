using Xunit;
using FluentAssertions;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Tests.Core.Aircraft.Systems;

public class FuelSystemTests
{
    [Fact]
    public void FuelSystem_CheckIgnitionRisk_ZwracaTruePrzyWielkimWycieku()
    {
        // Arrange
        var fuelSystem = new FuelSystem();
        
        // Act - Symulujemy gigantyczny wyciek (powyżej progu 150 kg/h)
        fuelSystem.StartLeak(160.0);

        // Assert
        fuelSystem.IsLeaking.Should().BeTrue();
        fuelSystem.CheckIgnitionRisk().Should().BeTrue();
    }

    [Fact]
    public void FuelSystem_CheckIgnitionRisk_ZwracaFalsePrzyMalymWycieku()
    {
        // Arrange
        var fuelSystem = new FuelSystem();
        
        // Act - Symulujemy mały wyciek (poniżej 150 kg/h)
        fuelSystem.StartLeak(50.0);

        // Assert
        fuelSystem.CheckIgnitionRisk().Should().BeFalse();
    }

    [Fact]
    public void FuelSystem_SealLeak_SkutecznieZatrzymujeWyciek()
    {
        // Arrange
        var fuelSystem = new FuelSystem();
        fuelSystem.StartLeak(100.0);
        
        // Act
        bool result = fuelSystem.SealLeak();
        
        // Assert
        result.Should().BeTrue(); // Metoda powinna zgłosić sukces
        fuelSystem.IsLeaking.Should().BeFalse();
        fuelSystem.CurrentLeakRate.Should().Be(0.0);
    }
}