namespace AeroSimulator.Core.Aircraft;

public class AutopilotSystem
{
    public bool IsEngaged { get; private set; }
    public void Disengage() => IsEngaged = false;
    public void SetTargetAltitude(double altitude) {}
    public void ResyncAltitude(double altitude) {}
}

public class ElectricalSystem
{
    public double MainBusVoltage { get; set; } = 28.0;
    public double SecondaryBusVoltage { get; set; } = 28.0;
    public bool ActivateDeIcing() => true;
    public bool SwitchToBackupBattery() => true;
}

public class HydraulicSystem
{
    public bool IsGearTransiting { get; set; }
    public bool GearJammed { get; set; }
    public double Pressure { get; set; } = 3000.0;
    public bool EmergencyGearExtension() => true;
}

public class FuelSystem
{
    public void StartLeak(double rateKgH) {}
    public bool CheckIgnitionRisk() => false;
    public bool SealLeak() => true;
}

public class NavigationSystem
{
    public void SetOffline() {}
}
