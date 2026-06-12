namespace AeroSimulator.Core.Aircraft.Systems;

public class ElectricalSystem : IAircraftSystem
{
    public double MainBusVoltage { get; private set; } = 28.0;
    public double SecondaryBusVoltage { get; private set; } = 28.0;
    public bool IsDeIcingActive { get; private set; }
    public bool IsOnBackupBattery { get; private set; }
    
    public bool IsOffline => MainBusVoltage <= 0 && SecondaryBusVoltage <= 0 && !IsOnBackupBattery;

    public void CutMainBus()
    {
        MainBusVoltage = 0.0;
        IsDeIcingActive = false;
    }

    public void CutSecondaryBus()
    {
        SecondaryBusVoltage = 0.0;
    }

    public bool SwitchToBackupBattery()
    {
        if (!IsOnBackupBattery)
        {
            IsOnBackupBattery = true;
            MainBusVoltage = 24.0; // Napięcie z baterii awaryjnej
            return true;
        }
        return false;
    }

    public bool ActivateDeIcing()
    {
        if (MainBusVoltage > 0 || IsOnBackupBattery)
        {
            IsDeIcingActive = true;
            return true;
        }
        return false;
    }

    public void SetOffline()
    {
        CutMainBus();
        CutSecondaryBus();
        IsOnBackupBattery = false;
    }

    public bool Reboot()
    {
        MainBusVoltage = 28.0;
        SecondaryBusVoltage = 28.0;
        IsOnBackupBattery = false;
        return true;
    }
}