namespace AeroSimulator.Core.Aircraft.Systems;

public class FuelSystem
{
    public double TotalFuelKg { get; set; } = 10000.0;
    public double LeakRateKgH { get; private set; }
    public bool HasLeak => LeakRateKgH > 0;

    public void StartLeak(double rateKgH)
    {
        LeakRateKgH = rateKgH;
    }

    public bool CheckIgnitionRisk()
    {
        return HasLeak; 
    }

    public bool SealLeak()
    {
        if (HasLeak)
        {
            LeakRateKgH = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Metoda do wywoływania w głównej pętli Update samolotu,
    /// by faktycznie ubywało paliwa w czasie wycieku.
    /// </summary>
    public void Update(double deltaT)
    {
        if (HasLeak)
        {
            TotalFuelKg -= (LeakRateKgH / 3600.0) * deltaT;
            if (TotalFuelKg < 0) TotalFuelKg = 0;
        }
    }
}