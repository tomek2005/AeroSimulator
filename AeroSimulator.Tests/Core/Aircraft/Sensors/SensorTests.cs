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
        // Arrange
        var sensor = new Sensor("TEST-SNS");

        // Act
        sensor.Kill();

        // Assert
        sensor.State.Should().Be(SensorState.Dead);
        sensor.Accuracy.Should().Be(0.0);
    }

    [Fact]
    public void Sensor_Read_MartwyCzujnikZwracaOptionNone()
    {
        // Arrange
        var sensor = new Sensor("TEST-SNS");
        sensor.Kill();

        // Act
        var result = sensor.Read(100.0);

        // Assert
        // Zamiast sprawdzać, czy to -1, weryfikujemy czy monada jest pusta:
        result.HasValue.Should().BeFalse();
        
        // Funkcyjne sprawdzenie, czy próba odczytu pustej monady rzuca właściwy wyjątek:
        Action act = () => { var val = result.Value; };
        act.Should().Throw<InvalidOperationException>().WithMessage("*pustej monady*");
    }

    [Fact]
    public void Sensor_ApplyDamage_ZmieniaStanNaFault()
    {
        // Arrange
        var sensor = new Sensor("TEST-SNS");

        // Act
        sensor.ApplyDamage(0.8);

        // Assert
        // Accuracy powinno wynosić 0.2 (używamy BeApproximately dla ułamków zmiennoprzecinkowych)
        sensor.Accuracy.Should().BeApproximately(0.2, 0.001);
        sensor.State.Should().Be(SensorState.Fault);
    }

    [Fact]
    public void Sensor_Repair_CzysciBledyPrzywracaZdrowie()
    {
        // Arrange
        var sensor = new Sensor("TEST-SNS");
        sensor.ApplyDamage(0.8); // Symulujemy uszkodzenie do stanu Fault

        // Act
        sensor.Repair();

        // Assert
        sensor.Accuracy.Should().Be(1.0);
        sensor.State.Should().Be(SensorState.OK);
    }
}