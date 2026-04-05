using Serilog;

namespace CoffeeUpdater.Utils;

public class InstallLogCollector
{

    public delegate void LogClearedHandler();
    public event LogClearedHandler? LogCleared;

    public delegate void LogAddedHandler(string message);
    public event LogAddedHandler? LogAdded;

    public void ClearLog()
    {
        LogCleared?.Invoke();
    }

    public void AddLog(string message)
    {
        Log.Information("InstallLog: {message}", message);
        LogAdded?.Invoke(message);
    }
}