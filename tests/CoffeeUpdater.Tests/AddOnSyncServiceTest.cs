using System.IO;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using CoffeeUpdater.Tests.Mocks;
using CoffeeUpdater.Utils;

namespace CoffeeUpdater.Tests;

[TestFixture]
public class AddOnSyncServiceTest
{
    private const string AddOnsPath = @"C:\World of Warcraft\_retail_\Interface\AddOns";

    private MockEnv _mockEnv = null!;
    private MockAddOnDownloader _mockDownloader = null!;
    private AddOnUpdateManager _updateManager = null!;
    private Config _config = null!;

    [SetUp]
    public void SetUp()
    {
        _mockEnv = new MockEnv();
        _mockDownloader = new MockAddOnDownloader();
        _config = new Config { AddOnsPath = AddOnsPath };
        var bundleInstaller = new AddOnBundleInstaller(_mockEnv, _config);
        var metadataLoader = new LocalAddOnMetadataLoader(_mockEnv, _config);
        var installLog = new InstallLogCollector();
        _updateManager = new AddOnUpdateManager(_mockDownloader, bundleInstaller, metadataLoader, installLog, _config, _mockEnv);
        _mockEnv.FileSystem.Directory.CreateDirectory(AddOnsPath);
    }

    [Test]
    public async Task RunSync_SuccessfulSync_FiresSyncCompleted()
    {
        var manifest = new AddOnManifest { AddOns = [] };
        _mockDownloader.SetManifest(manifest);

        var service = new AddOnSyncService(_updateManager, _config);

        var completed = false;
        service.SyncCompleted += () => completed = true;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // StartAsync triggers ExecuteAsync which does an initial sync
        await service.StartAsync(cts.Token);
        // Give the background task time to run
        await Task.Delay(500);
        await service.StopAsync(CancellationToken.None);

        Assert.That(completed, Is.True);
        Assert.That(service.LastSyncTime, Is.Not.Null);
        Assert.That(service.IsSyncing, Is.False);
    }

    [Test]
    public async Task RunSync_FailedSync_FiresSyncError()
    {
        _mockDownloader.SetShouldReturnNullManifest(true);

        var service = new AddOnSyncService(_updateManager, _config);

        string? errorMessage = null;
        service.SyncError += msg => errorMessage = msg;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await service.StartAsync(cts.Token);
        await Task.Delay(500);
        await service.StopAsync(CancellationToken.None);

        Assert.That(errorMessage, Is.Not.Null);
        Assert.That(errorMessage, Does.Contain("errors"));
    }

    [Test]
    public async Task RunSync_EmptyAddOnsPath_SkipsSync()
    {
        _config.AddOnsPath = "";
        var service = new AddOnSyncService(_updateManager, _config);

        var completed = false;
        service.SyncCompleted += () => completed = true;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await service.StartAsync(cts.Token);
        await Task.Delay(500);
        await service.StopAsync(CancellationToken.None);

        Assert.That(completed, Is.False);
        Assert.That(service.LastSyncTime, Is.Null);
    }

    [Test]
    public async Task PauseAndResume_BlocksSyncDuringPause()
    {
        var manifest = new AddOnManifest { AddOns = [] };
        _mockDownloader.SetManifest(manifest);

        var service = new AddOnSyncService(_updateManager, _config);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await service.StartAsync(cts.Token);
        await Task.Delay(200); // let initial sync complete

        await service.PauseAndWaitForCompletionAsync();

        Assert.That(service.IsSyncing, Is.False);

        service.Resume();

        await service.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task RunSync_UpdateManagerThrows_FiresSyncErrorWithExceptionMessage()
    {
        _mockDownloader.SetShouldThrow(true);

        var service = new AddOnSyncService(_updateManager, _config);

        string? errorMessage = null;
        service.SyncError += msg => errorMessage = msg;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await service.StartAsync(cts.Token);
        await Task.Delay(500);
        await service.StopAsync(CancellationToken.None);

        Assert.That(errorMessage, Is.Not.Null);
        Assert.That(errorMessage, Does.Contain("Sync failed"));
        Assert.That(service.IsSyncing, Is.False);
    }

    [Test]
    public async Task RunSync_PausedBeforeLockAcquire_SkipsSync()
    {
        var manifest = new AddOnManifest { AddOns = [] };
        _mockDownloader.SetManifest(manifest);

        var service = new AddOnSyncService(_updateManager, _config);

        // Pause before starting — when ExecuteAsync runs, the _paused
        // flag is already set, so RunSyncAsync returns early at line 65
        await service.PauseAndWaitForCompletionAsync();

        var completed = false;
        service.SyncCompleted += () => completed = true;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await service.StartAsync(cts.Token);
        await Task.Delay(500);

        // Sync should not have run
        Assert.That(completed, Is.False);
        Assert.That(service.LastSyncTime, Is.Null);

        service.Resume();
        await service.StopAsync(CancellationToken.None);
    }
}
