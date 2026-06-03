namespace AeroSimulator.Core.Events;

public interface IFlightEventHandler
{
    void Handle(FlightEvent evt);
}