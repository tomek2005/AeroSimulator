namespace AeroSimulator.Core.Aircraft.Systems;

public class HydraulicSystem : IAircraftSystem
{
    public double Pressure { get; private set; } = 3000.0;
    public bool IsGearTransiting { get; private set; }
    public bool GearJammed { get; private set; }
    public bool IsGearExtended { get; private set; } = true;

    public bool IsOffline => Pressure <= 0;

    public void StartGearTransit() => IsGearTransiting = true;
    public void StopGearTransit() => IsGearTransiting = false;

    public bool RetractGear()
    {
        if (GearJammed || Pressure <= 0) return false;

        IsGearExtended = false;
        IsGearTransiting = false;
        return true;
    }

    public void Depressurize()
    {
        Pressure = 0.0;
    }

    public void JamGear()
    {
        if (IsGearTransiting)
        {
            GearJammed = true;
        }
    }

    public bool EmergencyGearExtension()
    {
        // Grawitacyjne wypuszczenie podwozia działa nawet bez ciśnienia
        GearJammed = false;
        IsGearTransiting = false;
        IsGearExtended = true;
        return true;
    }

    public void SetOffline() => Depressurize();

    public bool Reboot()
    {
        Pressure = 3000.0;
        return true;
    }
}
