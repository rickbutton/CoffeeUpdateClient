using CoffeeUpdater.Models;
using CoffeeUpdater.Utils;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CoffeeUpdater.Services;

public class AddOnSyncService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly AddOnUpdateManager _updateManager;
    private readonly Config _config;

    public event Action? SyncStarted;
    public event Action? SyncCompleted;
    public event Action<string>? SyncError;

    public DateTime? LastSyncTime { get; private set; }
    public bool IsSyncing { get; private set; }

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private volatile bool _paused;

    public AddOnSyncService(AddOnUpdateManager updateManager, Config config)
    {
        _updateManager = updateManager;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("AddOnSyncService started, polling every {Interval}s", PollInterval.TotalSeconds);

        // Run an initial sync immediately
        await RunSyncAsync();

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSyncAsync();
        }
    }

    /// <summary>
    /// Pauses the sync service and waits for any in-progress sync to complete.
    /// The lock is held after this returns — no new syncs can start.
    /// Call Resume() if the update fails and syncing should continue.
    /// </summary>
    public async Task PauseAndWaitForCompletionAsync(CancellationToken ct = default)
    {
        _paused = true;
        // Acquire the lock and hold it — blocks until any running sync finishes
        await _syncLock.WaitAsync(ct);
        // Lock is intentionally NOT released — it will be held until Resume() or app exit
    }

    public void Resume()
    {
        _paused = false;
        _syncLock.Release();
    }

    private async Task RunSyncAsync()
    {
        if (_paused) return;

        if (string.IsNullOrEmpty(_config.AddOnsPath))
        {
            Log.Debug("AddOns path not configured, skipping sync");
            return;
        }

        if (!await _syncLock.WaitAsync(0)) return; // Already syncing or paused
        try
        {
            // Re-check after acquiring the lock to close the race with PauseAndWaitForCompletionAsync
            if (_paused) return;

            IsSyncing = true;
            SyncStarted?.Invoke();
            var success = await _updateManager.UpdateAddOns();
            LastSyncTime = DateTime.Now;

            if (success)
            {
                Log.Debug("Sync completed successfully");
            }
            else
            {
                Log.Warning("Sync completed with errors");
                SyncError?.Invoke("Sync completed with errors. See log for details.");
            }

            SyncCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception during addon sync");
            SyncError?.Invoke($"Sync failed: {ex.Message}");
        }
        finally
        {
            IsSyncing = false;
            _syncLock.Release();
        }
    }
}
