using System;
using Xunit;
using FluentAssertions;
using AeroSimulator.Core.Aircraft.Sensors;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Tests.Core.Aircraft.Sensors;

public class SensorTests
{
    [Fact]
    public void Sensor_Kill_ZmieniaStanNaMartwy()
    {
        var sensor = new Sensor("TEST-SNS");

        sensor.Kill();

        sensor.State.Should().Be(SensorState.Dead);
        sensor.Accuracy.Should().Be(0.0);
    }

    [Fact]
    public void Sensor_Read_MartwyCzujnikZwracaOptionNone()
    {
        var sensor = new Sensor("TEST-SNS");
        sensor.Kill();

        var result = sensor.Read(100.0);

        result.HasValue.Should().BeFalse();
        
        Action act = () => { var val = result.Value; };
        act.Should().Throw<InvalidOperationException>().WithMessage("*pustej monady*");
    }

    [Fact]
    public void Sensor_ApplyDamage_ZmieniaStanNaFault()
    {
        var sensor = new Sensor("TEST-SNS");

        sensor.ApplyDamage(0.8);

        sensor.Accuracy.Should().BeApproximately(0.2, 0.001);
        sensor.State.Should().Be(SensorState.Fault);
    }

    [Fact]
    public void Sensor_Repair_CzysciBledyPrzywracaZdrowie()
    {
        var sensor = new Sensor("TEST-SNS");
        sensor.ApplyDamage(0.8);

        sensor.Repair();

        sensor.Accuracy.Should().Be(1.0);
        sensor.State.Should().Be(SensorState.OK);
    }
}