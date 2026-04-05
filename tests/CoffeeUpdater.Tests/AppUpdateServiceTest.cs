using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using CoffeeUpdater.Tests.Mocks;
using CoffeeUpdater.Utils;
using NuGet.Versioning;
using Velopack;

namespace CoffeeUpdater.Tests;

[TestFixture]
public class AppUpdateServiceTest
{
    private MockAppUpdateManager _mockUpdateManager = null!;
    private AddOnSyncService _syncService = null!;
    private AppUpdateService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockUpdateManager = new MockAppUpdateManager();

        var env = new MockEnv();
        var config = new Config { AddOnsPath = @"C:\WoW\_retail_\Interface\AddOns" };
        env.FileSystem.Directory.CreateDirectory(config.AddOnsPath);
        var downloader = new MockAddOnDownloader();
        downloader.SetManifest(new AddOnManifest { AddOns = [] });
        var bundleInstaller = new AddOnBundleInstaller(env, config);
        var metadataLoader = new LocalAddOnMetadataLoader(env, config);
        var installLog = new InstallLogCollector();
        var addonUpdateManager = new AddOnUpdateManager(downloader, bundleInstaller, metadataLoader, installLog, config, env);
        _syncService = new AddOnSyncService(addonUpdateManager, config);

        _service = new AppUpdateService(_syncService, _mockUpdateManager);
        _service.ExitApplication = () => { };
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
        _syncService.Dispose();
    }

    private static UpdateInfo CreateUpdateInfo(string version = "2.0.0")
    {
        var asset = new VelopackAsset
        {
            PackageId = "CoffeeUpdater",
            Version = new SemanticVersion(2, 0, 0),
            FileName = "CoffeeUpdater-2.0.0-full.nupkg",
        };
        return new UpdateInfo(asset, false, null!, []);
    }

    [Test]
    public async Task CheckAndApply_NoUpdateAvailable_DoesNotDownload()
    {
        _mockUpdateManager.UpdateInfoToReturn = null;

        await _service.CheckAndApplyUpdateAsync(CancellationToken.None);

        Assert.That(_mockUpdateManager.CheckCallCount, Is.EqualTo(1));
        Assert.That(_mockUpdateManager.DownloadCallCount, Is.EqualTo(0));
        Assert.That(_mockUpdateManager.ApplyCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task CheckAndApply_UpdateAvailable_DownloadsAndApplies()
    {
        var exitCalled = false;
        _service.ExitApplication = () => exitCalled = true;
        _mockUpdateManager.UpdateInfoToReturn = CreateUpdateInfo();

        await _service.CheckAndApplyUpdateAsync(CancellationToken.None);

        Assert.That(_mockUpdateManager.CheckCallCount, Is.EqualTo(1));
        Assert.That(_mockUpdateManager.DownloadCallCount, Is.EqualTo(1));
        Assert.That(_mockUpdateManager.ApplyCallCount, Is.EqualTo(1));
        Assert.That(exitCalled, Is.True);
    }

    [Test]
    public async Task CheckAndApply_CheckThrows_DoesNotDownloadOrApply()
    {
        _mockUpdateManager.CheckException = new Exception("network error");

        await _service.CheckAndApplyUpdateAsync(CancellationToken.None);

        Assert.That(_mockUpdateManager.DownloadCallCount, Is.EqualTo(0));
        Assert.That(_mockUpdateManager.ApplyCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task CheckAndApply_DownloadThrows_DoesNotApply()
    {
        _mockUpdateManager.UpdateInfoToReturn = CreateUpdateInfo();
        _mockUpdateManager.DownloadException = new Exception("download failed");

        await _service.CheckAndApplyUpdateAsync(CancellationToken.None);

        Assert.That(_mockUpdateManager.DownloadCallCount, Is.EqualTo(1));
        Assert.That(_mockUpdateManager.ApplyCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task CheckAndApply_DownloadThrows_ResumesSyncService()
    {
        _mockUpdateManager.UpdateInfoToReturn = CreateUpdateInfo();
        _mockUpdateManager.DownloadException = new Exception("download failed");

        // Start the sync service so Resume() has something to resume
        await _syncService.StartAsync(CancellationToken.None);

        await _service.CheckAndApplyUpdateAsync(CancellationToken.None);

        // Sync service should still be running (resumed after failure)
        Assert.That(_syncService.ExecuteTask?.IsFaulted, Is.Not.True);

        await _syncService.StopAsync(CancellationToken.None);
    }

    [Test]
    public void CheckAndApply_Cancelled_DoesNotThrow()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockUpdateManager.UpdateInfoToReturn = CreateUpdateInfo();

        Assert.DoesNotThrowAsync(() => _service.CheckAndApplyUpdateAsync(cts.Token));
    }
}
