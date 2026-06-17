using Xunit;
using AeroSimulator.Core.Aircraft;

public class FlightDataTests
{
    [Fact]
    public void FlightData_FuelRemainingPercent_ZwracaPoprawnyProcent()
    {
        // Arrange (Sytuacja)
        var flightData = new FlightData(2) // Wymagane podanie liczby silników
        {
            FuelCapacityKg = 2000,
            FuelLevelKg = 500
        };

        // Act (Akcja)
        double result = flightData.FuelRemainingPercent();

        // Assert (Oczekiwany wynik)
        Assert.Equal(25.0, result);
    }

    [Fact]
    public void FlightData_IsStalling_ZwracaTrueGdyZbytWolno()
    {
        // Arrange
        var config = new AircraftConfig { StallSpeedFlaps = 120 };
        var flightData = new FlightData(2)
        {
            Config = config,
            Speed = 100,
            Altitude = 1000 // Zgodnie z implementacją, Altitude musi być > 0
        };

        // Act
        bool result = flightData.IsStalling();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void FlightData_IsOverspeed_ZwracaTrueGdyZaSzybko()
    {
        // Arrange
        var config = new AircraftConfig { MaxSpeedKts = 340 };
        var flightData = new FlightData(2)
        {
            Config = config,
            Speed = 350
        };

        // Act
        bool result = flightData.IsOverspeed();

        // Assert
        Assert.True(result);
    }
}