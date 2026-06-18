using Xunit;
using FluentAssertions;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Tests.Core.Aircraft.Systems;

public class EngineSystemTests
{
    [Fact]
    public void EngineUnit_Restart_ZwracaFalseGdyCiezkoUszkodzony()
    {
        var engineSystem = new EngineSystem(1);
        var engine = engineSystem.GetEngine(0);
        
        engine.ApplyDamage(0.9); 
        engine.Stop();
        
        bool result = engine.Restart();
        
        result.Should().BeFalse();
        engine.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void EngineUnit_Restart_ZwracaTrueGdyLekkoUszkodzony()
    {
        var engineSystem = new EngineSystem(1);
        var engine = engineSystem.GetEngine(0);
        
        engine.ApplyDamage(0.5); 
        engine.Stop();
        
        bool result = engine.Restart();
        
        result.Should().BeTrue();
        engine.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void EngineUnit_ExtinguishFire_GasiPozarTylkoGdyPlonal()
    {
        var engineSystem = new EngineSystem(1);
        var engine = engineSystem.GetEngine(0);
        
        engine.ExtinguishFire().Should().BeFalse();
        
        engine.StartFire();
        engine.IsOnFire.Should().BeTrue();
        
        engine.ExtinguishFire().Should().BeTrue();
        engine.IsOnFire.Should().BeFalse();
    }
}