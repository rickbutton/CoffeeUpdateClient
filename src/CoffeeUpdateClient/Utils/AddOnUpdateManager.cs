using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using Serilog;

namespace CoffeeUpdateClient.Utils;

public class AddOnUpdateManager
{

    private IAddOnDownloader _addOnDownloader;
    private AddOnBundleInstaller _addOnBundleInstaller;
    private LocalAddOnMetadataLoader _localAddOnMetadataLoader;
    private InstallLogCollector _installLog;

    private AddOnManifest? _latestManifest;
    private DateTime? _lastManifestTime;

    public AddOnUpdateManager(IAddOnDownloader addOnDownloader, AddOnBundleInstaller addOnBundleInstaller, LocalAddOnMetadataLoader localAddOnMetadataLoader, InstallLogCollector installLog)
    {
        _addOnDownloader = addOnDownloader;
        _addOnBundleInstaller = addOnBundleInstaller;
        _localAddOnMetadataLoader = localAddOnMetadataLoader;
        _installLog = installLog;
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
            if (!state.IsUpdated)
            {
                var verb = state.IsInstalled ? "update" : "install";
                _installLog.AddLog($"Starting {verb} of addon {state.Name}-{state.RemoteAddOn.Version}");

                var bundle = await _addOnDownloader.GetAddOnBundleAsync(state.RemoteAddOn);
                if (bundle == null)
                {
                    _installLog.AddLog($"Failed to fetch addon bundle {state.RemoteAddOn.Name}-{state.RemoteAddOn.Version} during update");
                    success = false;
                    continue;
                }

                if (!_addOnBundleInstaller.InstallAddOn(bundle))
                {
                    _installLog.AddLog($"Failed to {verb} addon bundle {state.RemoteAddOn.Name}-{state.RemoteAddOn.Version} during update");
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
            states.Add(new AddOnInstallState(localMetadata, remoteMetadata));
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