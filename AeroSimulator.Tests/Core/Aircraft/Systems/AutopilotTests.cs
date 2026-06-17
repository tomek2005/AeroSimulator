using Xunit;
using AeroSimulator.Core.Aircraft.Systems;

namespace AeroSimulator.Tests.Core.Aircraft.Systems;

public class AutopilotTests
{
    [Fact]
    public void AutopilotSystem_Engage_UstawiaFlage()
    {
        // Arrange
        var autopilot = new AutopilotSystem();

        // Act
        autopilot.Engage();

        // Assert
        Assert.True(autopilot.IsEngaged);
    }

    [Fact]
    public void AutopilotSystem_Engage_IgnorujeJesliOffline()
    {
        // Arrange
        var autopilot = new AutopilotSystem();
        
        // Act
        autopilot.SetOffline(); // Symulujemy brak prądu
        autopilot.Engage();     // Próbujemy włączyć

        // Assert
        Assert.False(autopilot.IsEngaged);
    }

    [Fact]
    public void Calculator_CalculateNewPitch_TrzymaLimitKonta()
    {
        // Arrange
        double currentPitch = 0.0;
        double targetAltitude = 20000.0;
        double sensedAltitude = 0.0;
        double deltaT = 1.0;

        // Act
        double resultUp = AutopilotCalculator.CalculateNewPitch(currentPitch, targetAltitude, sensedAltitude, deltaT);
        double resultDown = AutopilotCalculator.CalculateNewPitch(currentPitch, 0.0, targetAltitude, deltaT);

        // Assert
        Assert.Equal(8.0, resultUp);    // Oczekujemy obcięcia do +8.0
        Assert.Equal(-8.0, resultDown); // Oczekujemy obcięcia do -8.0
    }

    [Fact]
    public void Calculator_CalculateNewThrottle_TrzymaLimitPrzepustnicy()
    {
        // Arrange
        double currentThrottle = 0.5;
        double targetSpeed = 500.0;
        double sensedSpeed = 0.0;
        double deltaT = 1.0;

        // Act
        double resultMax = AutopilotCalculator.CalculateNewThrottle(currentThrottle, targetSpeed, sensedSpeed, deltaT);
        double resultMin = AutopilotCalculator.CalculateNewThrottle(0.1, 0.0, targetSpeed, deltaT);

        // Assert
        Assert.Equal(1.0, resultMax); // Oczekujemy obcięcia do 100% (1.0)
        Assert.Equal(0.0, resultMin); // Oczekujemy braku spadku poniżej zera (0.0)
    }
}