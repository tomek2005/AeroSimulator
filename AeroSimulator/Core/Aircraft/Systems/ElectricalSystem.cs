namespace AeroSimulator.Core.Aircraft.Systems;

public class ElectricalSystem
{
    public double MainBusVoltage { get; set; } = 28.0;
    public double SecondaryBusVoltage { get; set; } = 28.0;
    public bool IsDeIcingActive { get; private set; }
    public bool IsOnBackupBattery { get; private set; }

    public bool ActivateDeIcing()
    {
        if (MainBusVoltage > 0 || IsOnBackupBattery)
        {
            IsDeIcingActive = true;
            return true;
        }
        return false;
    }

    public bool SwitchToBackupBattery()
    {
        IsOnBackupBattery = true;
        MainBusVoltage = 24.0; // Napięcie awaryjne
        return true;
    }
}