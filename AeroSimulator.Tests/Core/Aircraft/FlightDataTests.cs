using Xunit;
using AeroSimulator.Core.Aircraft;

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