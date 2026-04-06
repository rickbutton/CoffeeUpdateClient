using CoffeeUpdater.Services;
using Velopack;

namespace CoffeeUpdater.Tests.Mocks;

public class MockAppUpdateManager : IAppUpdateManager
{
    public UpdateInfo? UpdateInfoToReturn { get; set; }
    public Exception? CheckException { get; set; }
    public Exception? DownloadException { get; set; }

    public int CheckCallCount { get; private set; }
    public int DownloadCallCount { get; private set; }
    public int ApplyCallCount { get; private set; }

    public Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        CheckCallCount++;
        if (CheckException != null) throw CheckException;
        return Task.FromResult(UpdateInfoToReturn);
    }

    public Task DownloadUpdatesAsync(UpdateInfo updateInfo)
    {
        DownloadCallCount++;
        if (DownloadException != null) throw DownloadException;
        return Task.CompletedTask;
    }

    public void WaitExitThenApplyUpdates(UpdateInfo updateInfo, bool silent, bool restart)
    {
        ApplyCallCount++;
    }
}
