namespace AeroSimulator.Infrastructure;

using AeroSimulator.Core.Aircraft;

public class AircraftFactory
{
    public static Aircraft CreateBoeing737(string tailNumber)
    {
        // Pełna, fizyczna specyfikacja samolotu wymagana przez klasę AircraftConfig
        var config = new AircraftConfig
        {
            // --- Identyfikacja ---
            TailNumber = tailNumber,
            DisplayName = "Boeing 737-800",
            
            // --- Osiągi i limity ---
            MaxAltitudeFt = 41000.0,      // Maksymalny pułap
            CruiseSpeedKts = 453.0,       // Prędkość przelotowa
            MaxSpeedKts = 473.0,          // Prędkość maksymalna
            MaxClimbRateFtMin = 3000.0,   // Maksymalna prędkość wznoszenia
            NormalDescentFtMin = 1500.0,  // Standardowa prędkość zniżania
            MaxCrosswindKts = 33.0,       // Maksymalny wiatr boczny
            
            // --- Prędkości startowe i lądowania (V-speeds) ---
            V1SpeedKts = 135.0,           // Prędkość decyzji (powyżej nie można przerwać startu)
            VRSpeedKts = 140.0,           // Prędkość rotacji (unoszenie nosa)
            V2SpeedKts = 150.0,           // Bezpieczna prędkość wznoszenia z awarią silnika
            StallSpeedKts = 150.0,        // Prędkość przeciągnięcia (bez klap)
            StallSpeedFlaps = 110.0,      // Prędkość przeciągnięcia (pełne klapy)
            
            // --- Napęd ---
            EngineCount = 2,
            MaxThrustKN = 120.0,          // Maksymalny ciąg (w kiloniutonach na silnik)
            
            // --- Paliwo ---
            MaxFuelKg = 26000.0,          // Pojemność zbiorników
            FuelBurnKgPerH = 2500.0,      // Spalanie na godzinę
            
            // --- Wytrzymałość strukturalna ---
            WingStrength = 1.0            // 100% wytrzymałości skrzydeł
        };
        
        // Przekazujemy w pełni skonfigurowany obiekt
        var aircraft = new Aircraft(tailNumber, "Boeing 737", config);
        
        // Konfiguracja początkowa na pasie startowym
        aircraft.FlightData.Altitude = 0.0;
        aircraft.FlightData.Speed = 0.0;
        aircraft.FlightData.Heading = 270.0; 
        aircraft.FlightData.FuelLevelKg = 15000.0; // Prawie 60% baku na start
        
        return aircraft;
    }
}