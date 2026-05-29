namespace AeroSimulator.Core.Aircraft;



//To nie jest zrobione tylko zeby bledow nie sypalo w innych plikach!




/// <summary>
/// Represents the true, perfect physical state of the aircraft at the current simulation tick.
/// This data comes straight from the physics engine before sensors add any noise or faults.
/// </summary>
public class FlightData
{
    /// <summary>True altitude in feet.</summary>
    public double Altitude { get; set; }

    /// <summary>True airspeed in knots.</summary>
    public double Speed { get; set; }

    /// <summary>True total fuel level remaining in kilograms.</summary>
    public double FuelLevelKg { get; set; }

    /// <summary>
    /// True RPM for each engine. 
    /// Size of the array matches the number of engines on the aircraft model.
    /// </summary>
    public double[] EngineRPMs { get; set; }

    /// <summary>
    /// True Exhaust Gas Temperature (EGT) in Celsius for each engine.
    /// Size of the array matches the number of engines on the aircraft model.
    /// </summary>
    public double[] EngineTempsC { get; set; }

    /// <summary>
    /// Initializes a new snapshot of flight data with allocated engine arrays.
    /// </summary>
    /// <param name="engineCount">The number of engines to allocate data slots for.</param>
    public FlightData(int engineCount)
    {
        if (engineCount < 1)
            throw new ArgumentException("Engine count must be at least 1.", nameof(engineCount));

        EngineRPMs = new double[engineCount];
        EngineTempsC = new double[engineCount];
    }
}