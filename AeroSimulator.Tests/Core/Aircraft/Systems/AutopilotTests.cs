using Xunit;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Tests.Core.Aircraft.Systems;

// Testy, które sprawdzają kolejno czy:
// 1. Metoda Engage() poprawnie włącza autopilota (ustawia flagę IsEngaged na true).
// 2. Autopilot ignoruje polecenie włączenia, jeśli system jest odcięty od zasilania (Offline).
// 3. Kalkulator lotu bezpiecznie ogranicza kąt pochylenia maszyny do fizycznych limitów (-8.0 do +8.0 stopni).
// 4. Kalkulator lotu poprawnie trzyma moc przepustnicy w limitach (nie mniej niż 0% i nie więcej niż 100%).

public class AutopilotTests
{
    [Fact]
    public void AutopilotSystem_Engage_UstawiaFlage()
    {
        var autopilot = new AutopilotSystem();

        autopilot.Engage();

        Assert.True(autopilot.IsEngaged);
    }

    [Fact]
    public void AutopilotSystem_Engage_IgnorujeJesliOffline()
    {

        var autopilot = new AutopilotSystem();
        
        autopilot.SetOffline();
        autopilot.Engage();

        Assert.False(autopilot.IsEngaged);
    }

    [Fact]
    public void Calculator_CalculateNewPitch_TrzymaLimitKonta()
    {
        double currentPitch = 0.0;
        double targetAltitude = 20000.0;
        double sensedAltitude = 0.0;
        double deltaT = 1.0;

        double resultUp = AutopilotCalculator.CalculateNewPitch(currentPitch, targetAltitude, sensedAltitude, deltaT);
        double resultDown = AutopilotCalculator.CalculateNewPitch(currentPitch, 0.0, targetAltitude, deltaT);

        Assert.Equal(8.0, resultUp);
        Assert.Equal(-8.0, resultDown);
    }

    [Fact]
    public void Calculator_CalculateNewThrottle_TrzymaLimitPrzepustnicy()
    {
        double currentThrottle = 0.5;
        double targetSpeed = 500.0;
        double sensedSpeed = 0.0;
        double deltaT = 1.0;

        double resultMax = AutopilotCalculator.CalculateNewThrottle(currentThrottle, targetSpeed, sensedSpeed, deltaT);
        double resultMin = AutopilotCalculator.CalculateNewThrottle(0.1, 0.0, targetSpeed, deltaT);

        Assert.Equal(1.0, resultMax);
        Assert.Equal(0.0, resultMin);
    }
}