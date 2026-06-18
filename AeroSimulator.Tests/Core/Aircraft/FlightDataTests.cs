using Xunit;
using AeroSimulator.Core.Aircraft;

// Testy, które sprawdzają kolejno czy:
// 1. Poziom paliwa jest poprawnie przeliczany na wartość procentową.
// 2. IsStalling() zwraca true, jeśli prędkość samolotu jest za niska (poniżej 120 węzłów).
// 3. IsOverspeed() zwraca true, jeśli przekroczono maksymalną prędkość (powyżej 340 węzłów).


public class FlightDataTests
{
    [Fact]
    public void FlightData_FuelRemainingPercent_ZwracaPoprawnyProcent()
    {
        var flightData = new FlightData(2)
        {
            FuelCapacityKg = 2000,
            FuelLevelKg = 500
        };

        double result = flightData.FuelRemainingPercent();

        Assert.Equal(25.0, result);
    }

    [Fact]
    public void FlightData_IsStalling_ZwracaTrueGdyZbytWolno()
    {
        var config = new AircraftConfig { StallSpeedFlaps = 120 };
        var flightData = new FlightData(2)
        {
            Config = config,
            Speed = 100,
            Altitude = 1000
        };

        bool result = flightData.IsStalling();

        Assert.True(result);
    }

    [Fact]
    public void FlightData_IsOverspeed_ZwracaTrueGdyZaSzybko()
    {
        var config = new AircraftConfig { MaxSpeedKts = 340 };
        var flightData = new FlightData(2)
        {
            Config = config,
            Speed = 350
        };

        bool result = flightData.IsOverspeed();

        Assert.True(result);
    }
}