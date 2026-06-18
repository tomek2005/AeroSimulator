using System;
using AeroSimulator.Core.Aircraft;
using AeroSimulator.Core.Aircraft.Enums;

namespace AeroSimulator.Core.Strategies.Anomalies;

using Aircraft = AeroSimulator.Core.Aircraft.Aircraft;

// Defines the contract for all flight anomalies that can occur during simulation.
public interface IAnomaly
{
    string AnomalyName { get; }
    string Description { get; }
    Severity Level { get; }
    double Probability { get; }
    bool IsActive { get; }
    bool CanBeResolved { get; }

    void Trigger(Aircraft ctx, FlightData data);
    void Update(Aircraft ctx, FlightData data, double deltaT);
    bool Resolve(Aircraft ctx);

    string GetWarningMessage();
    string GetPilotAction();
}