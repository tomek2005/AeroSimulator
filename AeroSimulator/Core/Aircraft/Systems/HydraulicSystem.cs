namespace AeroSimulator.Core.Aircraft.Systems;

public class HydraulicSystem
{
    public bool IsGearTransiting { get; set; }
    public bool GearJammed { get; set; }
    public double Pressure { get; set; } = 3000.0;
    public bool IsEmergencyGearExtended { get; private set; }

    public bool EmergencyGearExtension()
    {
        IsEmergencyGearExtended = true;
        GearJammed = false;
        return true;
    }
}