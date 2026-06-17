using Xunit;
using FluentAssertions;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Tests.Core.Aircraft.Systems;

public class EngineSystemTests
{
    [Fact]
    public void EngineUnit_Restart_ZwracaFalseGdyCiezkoUszkodzony()
    {
        // Arrange
        var engineSystem = new EngineSystem(1);
        var engine = engineSystem.GetEngine(0);
        
        // Act - Niszczymy silnik tak, by jego Health spadło poniżej 0.2
        engine.ApplyDamage(0.9); // Zdrowie spada z 1.0 na 0.1
        engine.Stop();
        
        bool result = engine.Restart();
        
        // Assert
        result.Should().BeFalse(); // Oczekujemy odrzucenia startu
        engine.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void EngineUnit_Restart_ZwracaTrueGdyLekkoUszkodzony()
    {
        // Arrange
        var engineSystem = new EngineSystem(1);
        var engine = engineSystem.GetEngine(0);
        
        // Act - Lekkie uszkodzenie (Zdrowie 0.5)
        engine.ApplyDamage(0.5); 
        engine.Stop();
        
        bool result = engine.Restart();
        
        // Assert
        result.Should().BeTrue(); // Silnik rzęzi, ale odpala!
        engine.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void EngineUnit_ExtinguishFire_GasiPozarTylkoGdyPlonal()
    {
        // Arrange
        var engineSystem = new EngineSystem(1);
        var engine = engineSystem.GetEngine(0);
        
        // Act & Assert 1: Próba zgaszenia zdrowego silnika
        engine.ExtinguishFire().Should().BeFalse();
        
        // Act & Assert 2: Podpalamy i gasimy
        engine.StartFire();
        engine.IsOnFire.Should().BeTrue();
        
        engine.ExtinguishFire().Should().BeTrue();
        engine.IsOnFire.Should().BeFalse();
    }
}