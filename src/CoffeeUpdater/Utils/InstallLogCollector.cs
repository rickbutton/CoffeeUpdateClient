using Serilog;

namespace CoffeeUpdater.Utils;

public class InstallLogCollector
{

    public delegate void LogClearedHandler();
    public event LogClearedHandler? LogCleared;

    public delegate void LogAddedHandler(string message);
    public event LogAddedHandler? LogAdded;

    public delegate void UpdatesAppliedHandler(int count);
    public event UpdatesAppliedHandler? UpdatesApplied;

    public void ClearLog()
    {
        LogCleared?.Invoke();
    }

    public void AddLog(string message)
    {
        Log.Information("InstallLog: {message}", message);
        LogAdded?.Invoke(message);
    }

    public void NotifyUpdatesApplied(int count)
    {
        UpdatesApplied?.Invoke(count);
    }
}
