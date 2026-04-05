using System.IO.Abstractions;
using System.Linq;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using Serilog;

namespace CoffeeUpdater.Utils;

public class AddOnUpdateManager
{
    private readonly IAddOnDownloader _addOnDownloader;
    private readonly AddOnBundleInstaller _addOnBundleInstaller;
    private readonly LocalAddOnMetadataLoader _localAddOnMetadataLoader;
    private readonly InstallLogCollector _installLog;
    private readonly Config _config;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly IFileSystem _fileSystem;

    public AddOnUpdateManager(IAddOnDownloader addOnDownloader, AddOnBundleInstaller addOnBundleInstaller, LocalAddOnMetadataLoader localAddOnMetadataLoader, InstallLogCollector installLog, Config config, IEnv env)
    {
        _addOnDownloader = addOnDownloader;
        _addOnBundleInstaller = addOnBundleInstaller;
        _localAddOnMetadataLoader = localAddOnMetadataLoader;
        _installLog = installLog;
        _config = config;
        _fileSystem = env.FileSystem;
    }

    public async Task<bool> UpdateAddOns()
    {
        await _operationLock.WaitAsync();
        try
        {
            var result = await _addOnDownloader.GetLatestManifestAsync();

            if (result.Status == ManifestResult.ResultStatus.Failed || result.Manifest == null)
            {
                Log.Error("failed to get addon manifest while attempting to update addons");
                return false;
            }

            if (result.Status == ManifestResult.ResultStatus.NotModified)
            {
                Log.Debug("manifest not modified, skipping update");
                return true;
            }

            var installStates = await GetAddOnInstallStatesAsync(result.Manifest);
            return await ApplyUpdatesAsync(installStates);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<IEnumerable<AddOnInstallState>?> GetAddOnInstallStatesForLatestManifestAsync()
    {
        await _operationLock.WaitAsync();
        try
        {
            var result = await _addOnDownloader.GetLatestManifestAsync();

            if (result.Status == ManifestResult.ResultStatus.Failed || result.Manifest == null)
            {
                Log.Error("failed to get addon manifest while computing install states");
                return null;
            }
            return await GetAddOnInstallStatesAsync(result.Manifest);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<IEnumerable<AddOnInstallState>> GetAddOnInstallStatesAsync(AddOnManifest manifest)
    {
        var states = new List<AddOnInstallState>();
        foreach (var remoteMetadata in manifest.AddOns)
        {
            var (localMetadata, localMetadataStatus) = await _localAddOnMetadataLoader.LoadAddOnMetadataAsync(remoteMetadata.Name);
            var hasLocalError = localMetadataStatus == LocalAddOnMetadataLoader.Status.Error;
            states.Add(new AddOnInstallState(localMetadata, remoteMetadata, hasLocalError));
        }

        return states;
    }

    private async Task<bool> ApplyUpdatesAsync(IEnumerable<AddOnInstallState> installStates)
    {
        bool success = true;
        foreach (var state in installStates)
        {
            if (state.ShouldUninstall)
            {
                var hasTrackedFolders = _config.InstalledAddOnFolders.ContainsKey(state.Name);
                if (!state.IsInstalled && !hasTrackedFolders)
                {
                    continue;
                }

                try
                {
                    _addOnBundleInstaller.UninstallAddOn(state.Name);
                    _installLog.AddLog($"AddOn {state.Name} removal successful");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception uninstalling addon {Name}", state.Name);
                    _installLog.AddLog($"Failed to remove addon {state.Name}: {ex.Message}");
                    success = false;
                }
                continue;
            }

            if (!state.IsUpdated)
            {
                var verb = state.HasLocalError ? "reinstall" : state.IsInstalled ? "update" : "install";
                _installLog.AddLog($"Starting {verb} of addon {state.Name}-{state.RemoteAddOn.Version}");

                var bundle = await _addOnDownloader.GetAddOnBundleAsync(state.RemoteAddOn);
                if (bundle == null)
                {
                    _installLog.AddLog($"Failed to fetch addon bundle {state.RemoteAddOn.Name}-{state.RemoteAddOn.Version} during update");
                    success = false;
                    continue;
                }

                try
                {
                    var installedFolders = _addOnBundleInstaller.InstallAddOn(bundle);

                    var prevFolders = _config.InstalledAddOnFolders.GetValueOrDefault(state.Name, [state.Name]);
                    var orphans = prevFolders.Except(installedFolders).ToList();
                    foreach (var orphan in orphans)
                    {
                        var orphanPath = _fileSystem.Path.Combine(_config.AddOnsPath, orphan);
                        if (_fileSystem.Directory.Exists(orphanPath))
                        {
                            _fileSystem.Directory.Delete(orphanPath, true);
                            Log.Information("Removed orphaned addon folder '{Folder}'", orphan);
                        }
                    }

                    _config.SetInstalledFolders(state.Name, installedFolders);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception installing addon {Name}-{Version}", state.RemoteAddOn.Name, state.RemoteAddOn.Version);
                    _installLog.AddLog($"Failed to {verb} addon bundle {state.RemoteAddOn.Name}-{state.RemoteAddOn.Version}: {ex.Message}");
                    success = false;
                    continue;
                }

                _installLog.AddLog($"AddOn {state.RemoteAddOn.Name}-{state.RemoteAddOn.Version} {verb} successful");
            }
            else
            {
                _installLog.AddLog($"AddOn {state.RemoteAddOn.Name} skipped, already up to date.");
            }
        }

        return success;
    }
}
