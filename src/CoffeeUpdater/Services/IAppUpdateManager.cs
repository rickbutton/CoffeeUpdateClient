using Velopack;

namespace CoffeeUpdater.Services;

public interface IAppUpdateManager
{
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task DownloadUpdatesAsync(UpdateInfo updateInfo);
    void ApplyUpdatesAndRestart(UpdateInfo updateInfo);
}
