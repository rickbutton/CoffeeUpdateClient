using Microsoft.Extensions.Hosting;
using Serilog;
using Velopack;

namespace CoffeeUpdater.Services;

public class AppUpdateService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private readonly AddOnSyncService _syncService;
    private readonly UpdateManager _updateManager;

    public AppUpdateService(AddOnSyncService syncService)
    {
        _syncService = syncService;
        var source = new Velopack.Sources.SimpleWebSource("https://coffee-auras.nyc3.digitaloceanspaces.com/releases");
        _updateManager = new UpdateManager(source);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("AppUpdateService started, checking every {Interval}m", CheckInterval.TotalMinutes);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAndApplyUpdateAsync(stoppingToken);
        }
    }

    private async Task CheckAndApplyUpdateAsync(CancellationToken ct)
    {
        bool paused = false;
        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo == null) return;

            Log.Information("App update available: {Version}", updateInfo.TargetFullRelease.Version);
            await _updateManager.DownloadUpdatesAsync(updateInfo);

            // Pause the sync service and wait for any in-progress sync to finish
            Log.Information("App update downloaded, pausing addon sync before applying");
            await _syncService.PauseAndWaitForCompletionAsync(ct);
            paused = true;

            Log.Information("Applying app update and restarting");
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
        }
        catch (OperationCanceledException)
        {
            // App shutting down, nothing to do
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for or apply app updates");
            if (paused) _syncService.Resume();
        }
    }
}
