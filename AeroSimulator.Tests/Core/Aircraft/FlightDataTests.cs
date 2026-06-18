using Xunit;
using AeroSimulator.Core.Aircraft;

// Testy, które sprawdzają kolejno czy:
// 1. Poziom paliwa jest poprawnie przeliczany na wartość procentową.
// 2. IsStalling() zwraca true, jeśli prędkość samolotu jest za niska (poniżej 120 węzłów).
// 3. IsOverspeed() zwraca true, jeśli przekroczono maksymalną prędkość (powyżej 340 węzłów).
// 4. IsInLandingZone() zwraca true, gdy wszystkie warunki do lądowania (dystans, wysokość, prędkość) są spełnione.
// 5. IsInLandingZone() zwraca false i odrzuca lądowanie, gdy samolot jest zbyt daleko od lotniska (powyżej 5 mil).
// 6. UpdateNavigation() poprawnie zmniejsza dystans do celu na podstawie upływającego czasu i prędkości statku.

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


    [Fact]
    public void FlightData_IsInLandingZone_ZwracaTrueGdyWarunkiSpelnione()
    {
        var flightData = new FlightData(1)
        {
            DistanceToDestinationNm = 2.0, // Tu może zostać 2.0 (jest <= 5.0)
            Altitude = 1000.0,
            AirportElevation = 100.0,
            Speed = 140.0
        };

        bool result = flightData.IsInLandingZone();

        Assert.True(result);
    }

    [Fact]
    public void FlightData_IsInLandingZone_ZwracaFalseGdyZbytDalekoLeczInneWymogiSpelnione()
    {
        var flightData = new FlightData(1)
        {
            DistanceToDestinationNm = 6.0, // ZMIANA: Zmienione na 6.0, ponieważ 5.0 to teraz poprawna strefa!
            Altitude = 1000.0,                 
            AirportElevation = 0.0,
            Speed = 130.0                      
        };

        bool result = flightData.IsInLandingZone();

        Assert.False(result); // Teraz test znów poprawnie sprawdzi blokadę lądowania
    }

    [Fact]
    public void FlightData_UpdateNavigation_PoprawnieZmniejszaDystansWZaleznosciOdCzasu()
    {
        var flightData = new FlightData(1)
        {
            DistanceToDestinationNm = 10.0,
            Speed = 360.0
        };

        flightData.UpdateNavigation(1.0);

        Assert.Equal(9.9, flightData.DistanceToDestinationNm, 3);
    }
}