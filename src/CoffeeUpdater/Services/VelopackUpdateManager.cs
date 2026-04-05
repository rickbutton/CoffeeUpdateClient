using Velopack;

namespace CoffeeUpdater.Services;

public class VelopackUpdateManager : IAppUpdateManager
{
    private readonly UpdateManager _updateManager;

    public VelopackUpdateManager()
    {
        var source = new Velopack.Sources.SimpleWebSource("https://coffee-auras.nyc3.digitaloceanspaces.com/releases");
        _updateManager = new UpdateManager(source);
    }

    public Task<UpdateInfo?> CheckForUpdatesAsync()
        => _updateManager.CheckForUpdatesAsync();

    public Task DownloadUpdatesAsync(UpdateInfo updateInfo)
        => _updateManager.DownloadUpdatesAsync(updateInfo);

    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
        => _updateManager.ApplyUpdatesAndRestart(updateInfo);
}
