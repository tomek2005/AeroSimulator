namespace AeroSimulator.Core.Events.Handlers;

public class AlertBufferHandler : IFlightEventHandler
{
    private static readonly List<string> _logs = new();
    private static readonly object _lock = new();

    public static IReadOnlyList<string> RecentLogs
    {
        get { lock (_lock) return _logs.ToArray(); }
    }

    public static void Clear()
    {
        lock (_lock) _logs.Clear();
    }

    public void Handle(FlightEvent evt)
    {
        string? formattedMessage = null;

        switch (evt)
        {
            case AnomalyTriggeredEvent anomaly:
                formattedMessage = $"[ALERT] {anomaly.Source}: {anomaly.Message}";
                break;
            case CascadeTriggeredEvent cascade:
                formattedMessage = $"[CRITICAL] CASCADE: {cascade.Message}";
                break;
            case SystemFailureEvent failure:
                formattedMessage = $"[WARN] FAILURE: {failure.Message}";
                break;
            case StateChangedEvent state:
                formattedMessage = $"[INFO] STATE: {state.NewState}";
                break;
        }

        if (formattedMessage != null)
        {
            lock (_lock)
            {
                _logs.Add($"[{evt.Timestamp:HH:mm:ss}] {formattedMessage}");
                if (_logs.Count > 3) _logs.RemoveAt(0); // Trzyma tylko ostatnie 3 dla UI
            }
        }
    }
}
