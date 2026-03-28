using System.IO.Abstractions;
using System.Linq;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using Serilog;

namespace CoffeeUpdateClient.Utils;

public class AddOnUpdateManager
{
    private readonly IAddOnDownloader _addOnDownloader;
    private readonly AddOnBundleInstaller _addOnBundleInstaller;
    private readonly LocalAddOnMetadataLoader _localAddOnMetadataLoader;
    private readonly InstallLogCollector _installLog;
    private readonly Config _config;
    private readonly IFileSystem _fileSystem;

    private AddOnManifest? _latestManifest;
    private DateTime? _lastManifestTime;

    public AddOnUpdateManager(IAddOnDownloader addOnDownloader, AddOnBundleInstaller addOnBundleInstaller, LocalAddOnMetadataLoader localAddOnMetadataLoader, InstallLogCollector installLog, Config config, IEnv env)
    {
        _addOnDownloader = addOnDownloader;
        _addOnBundleInstaller = addOnBundleInstaller;
        _localAddOnMetadataLoader = localAddOnMetadataLoader;
        _installLog = installLog;
        _config = config;
        _fileSystem = env.FileSystem;
    }

    public async Task<bool> UpdateAddOns(bool forceRefresh = false)
    {
        var manifest = await RefreshManifestAsync(forceRefresh);

        if (manifest == null)
        {
            Log.Error("failed to get addon manifest while attempting to update addons");
            return false;
        }

        var installStates = await GetAddOnInstallStatesAsync(manifest);

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

    public async Task<IEnumerable<AddOnInstallState>?> GetAddOnInstallStatesForLatestManifestAsync(bool forceRefresh)
    {
        var manifest = await RefreshManifestAsync(forceRefresh);

        if (manifest == null)
        {
            Log.Error("failed to get addon manifest while computing install states");
            return null;
        }
        return await GetAddOnInstallStatesAsync(manifest);
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

    private async Task<AddOnManifest?> RefreshManifestAsync(bool force = false)
    {
        var expireTime = DateTime.Now - TimeSpan.FromMinutes(5);
        if (force || _lastManifestTime == null || _lastManifestTime <= expireTime)
        {
            if (force)
            {
                Log.Information("forcing refresh of manifest");
            }
            else
            {
                Log.Information("latest manifest is expired, fetching new manifest");
            }

            var manifest = await _addOnDownloader.GetLatestManifestAsync();
            if (manifest != null)
            {
                _latestManifest = manifest;
                _lastManifestTime = DateTime.Now;
            }
            return manifest;
        }
        else
        {
            Log.Information("manifest already exists and isn't expired, using existing manifest");
            return _latestManifest;
        }
    }
}
