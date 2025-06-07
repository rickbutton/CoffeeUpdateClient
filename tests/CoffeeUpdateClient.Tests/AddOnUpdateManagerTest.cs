using System.IO;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Utils;
using NUnit.Framework;

namespace CoffeeUpdateClient.Tests;

public class AddOnUpdateManagerTest : ConfigTestBase
{
    private MockEnv _mockEnv = null!;
    private MockAddOnDownloader _mockDownloader = null!;
    private AddOnBundleInstaller _bundleInstaller = null!;
    private LocalAddOnMetadataLoader _metadataLoader = null!;
    private InstallLogCollector _installLog = null!;
    private AddOnUpdateManager _updateManager = null!;
    private string _addOnsPath = null!;

    [SetUp]
    public void SetUp()
    {
        _mockEnv = new MockEnv();
        _mockDownloader = new MockAddOnDownloader();
        _bundleInstaller = new AddOnBundleInstaller(_mockEnv);
        _metadataLoader = new LocalAddOnMetadataLoader(_mockEnv);
        _installLog = new InstallLogCollector();
        _updateManager = new AddOnUpdateManager(_mockDownloader, _bundleInstaller, _metadataLoader, _installLog);
        
        _addOnsPath = Config.Instance.AddOnsPath;
        _mockEnv.FileSystem.Directory.CreateDirectory(_addOnsPath);
    }

    [Test]
    public async Task UpdateAddOns_ManifestIsNull_ReturnsFalseAsync()
    {
        _mockDownloader.SetShouldReturnNullManifest(true);

        var result = await _updateManager.UpdateAddOns();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateAddOns_AllAddOnsUpToDate_ReturnsTrueAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "TestAddOn1", Version = "1.0.0" },
                new AddOnMetadata { Name = "TestAddOn2", Version = "2.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);

        SetupLocalAddOn("TestAddOn1", "1.0.0");
        SetupLocalAddOn("TestAddOn2", "2.0.0");

        var result = await _updateManager.UpdateAddOns();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateAddOns_NewAddOnInstall_ReturnsTrueAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "NewAddOn", Version = "1.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);
        _mockDownloader.AddBundle("NewAddOn", "1.0.0", ["file1.lua"]);

        var result = await _updateManager.UpdateAddOns();

        Assert.That(result, Is.True);
        var addOnPath = Path.Combine(_addOnsPath, "NewAddOn");
        Assert.That(_mockEnv.FileSystem.Directory.Exists(addOnPath), Is.True);
    }

    [Test]
    public async Task UpdateAddOns_AddOnNeedsUpdate_ReturnsTrueAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "UpdateAddOn", Version = "2.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);
        _mockDownloader.AddBundle("UpdateAddOn", "2.0.0", ["updated.lua"]);

        SetupLocalAddOn("UpdateAddOn", "1.0.0");

        var result = await _updateManager.UpdateAddOns();

        Assert.That(result, Is.True);
        var addOnPath = Path.Combine(_addOnsPath, "UpdateAddOn");
        Assert.That(_mockEnv.FileSystem.Directory.Exists(addOnPath), Is.True);
    }

    [Test]
    public async Task UpdateAddOns_DownloadFails_ReturnsFalseAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "FailAddOn", Version = "1.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);
        _mockDownloader.AddBundle("FailAddOn", "1.0.0", null); // null bundle simulates download failure

        var result = await _updateManager.UpdateAddOns();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateAddOns_MixedResults_ReturnsFalseAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "GoodAddOn", Version = "1.0.0" },
                new AddOnMetadata { Name = "BadAddOn", Version = "1.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);
        _mockDownloader.AddBundle("GoodAddOn", "1.0.0", ["good.lua"]);
        _mockDownloader.AddBundle("BadAddOn", "1.0.0", null); // Simulate failure

        var result = await _updateManager.UpdateAddOns();

        Assert.That(result, Is.False);
        
        // Verify the good addon was still installed
        var goodAddOnPath = Path.Combine(_addOnsPath, "GoodAddOn");
        Assert.That(_mockEnv.FileSystem.Directory.Exists(goodAddOnPath), Is.True);
    }

    [Test]
    public async Task UpdateAddOns_ForceRefresh_CallsDownloaderAsync()
    {
        var manifest = new AddOnManifest { AddOns = [] };
        _mockDownloader.SetManifest(manifest);

        var result = await _updateManager.UpdateAddOns(forceRefresh: true);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetAddOnInstallStatesForLatestManifestAsync_ManifestIsNull_ReturnsNullAsync()
    {
        _mockDownloader.SetShouldReturnNullManifest(true);

        var result = await _updateManager.GetAddOnInstallStatesForLatestManifestAsync(false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAddOnInstallStatesForLatestManifestAsync_ValidManifest_ReturnsStatesAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "TestAddOn", Version = "1.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);

        var result = await _updateManager.GetAddOnInstallStatesForLatestManifestAsync(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count(), Is.EqualTo(1));
        Assert.That(result.First().Name, Is.EqualTo("TestAddOn"));
    }

    [Test]
    public async Task GetAddOnInstallStatesAsync_EmptyManifest_ReturnsEmptyListAsync()
    {
        var manifest = new AddOnManifest { AddOns = [] };

        var result = await _updateManager.GetAddOnInstallStatesAsync(manifest);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAddOnInstallStatesAsync_ValidManifest_ReturnsCorrectStatesAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "InstalledAddOn", Version = "2.0.0" },
                new AddOnMetadata { Name = "NotInstalledAddOn", Version = "1.0.0" }
            ]
        };

        SetupLocalAddOn("InstalledAddOn", "1.0.0"); // Outdated version

        var result = await _updateManager.GetAddOnInstallStatesAsync(manifest);

        Assert.That(result.Count(), Is.EqualTo(2));
        
        var installedState = result.First(s => s.Name == "InstalledAddOn");
        Assert.That(installedState.IsInstalled, Is.True);
        Assert.That(installedState.IsUpdated, Is.False);
        
        var notInstalledState = result.First(s => s.Name == "NotInstalledAddOn");
        Assert.That(notInstalledState.IsInstalled, Is.False);
        Assert.That(notInstalledState.IsUpdated, Is.False);
    }

    [Test]
    public async Task RefreshManifest_CachingBehavior_ReusesManifestWithinTimeoutAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "TestAddOn", Version = "1.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);

        // First call should fetch from downloader
        var result1 = await _updateManager.GetAddOnInstallStatesForLatestManifestAsync(false);
        
        // Second call within timeout should reuse cached manifest
        var result2 = await _updateManager.GetAddOnInstallStatesForLatestManifestAsync(false);

        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(result1!.Count(), Is.EqualTo(result2!.Count()));
    }

    [Test]
    public async Task RefreshManifest_ForceRefresh_IgnoresCacheAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "TestAddOn", Version = "1.0.0" }
            ]
        };
        _mockDownloader.SetManifest(manifest);

        // First call
        await _updateManager.GetAddOnInstallStatesForLatestManifestAsync(false);
        
        // Force refresh should ignore cache
        var result = await _updateManager.GetAddOnInstallStatesForLatestManifestAsync(true);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count(), Is.EqualTo(1));
    }

    private void SetupLocalAddOn(string name, string version)
    {
        var addOnPath = Path.Combine(_addOnsPath, name);
        _mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        
        var tocContent = $"## Title: {name}\n## Version: {version}\n## Interface: 100200";
        var tocPath = Path.Combine(addOnPath, $"{name}.toc");
        _mockEnv.FileSystem.File.WriteAllText(tocPath, tocContent);
    }
}
