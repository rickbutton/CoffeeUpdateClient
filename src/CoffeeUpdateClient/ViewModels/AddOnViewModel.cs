using System.Reactive;
using System.Reactive.Linq;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Utils;
using ReactiveUI;
using Serilog;

namespace CoffeeUpdateClient.ViewModels
{
    public class AddOnViewModel : ReactiveObject
    {
        public enum InstallResult {
            Success,
            Error,
        }
        
        public string? AddOnsPath { get; }
        public AddOnMetadata RemoteMetadata { get; }

        private readonly IAddOnDownloader _addOnDownloader;
        private readonly AddOnBundleInstaller _AddOnBundleInstaller;

        private readonly ObservableAsPropertyHelper<AddOnMetadata?> _localMetadata;
        public AddOnMetadata? LocalMetadata => _localMetadata.Value;
        private readonly ObservableAsPropertyHelper<LocalAddOnMetadataLoader.Status> _localMetadataStatus;
        public LocalAddOnMetadataLoader.Status LocalMetadataStatus => _localMetadataStatus.Value;

        private readonly ObservableAsPropertyHelper<string> _localMetadataString;
        public string LocalMetadataString => _localMetadataString.Value;

        private readonly ObservableAsPropertyHelper<string> _requiredAction;
        public string RequiredAction => _requiredAction.Value;

        private readonly ObservableAsPropertyHelper<bool> _canInstallAddOn;
        public bool CanInstallAddOn => _canInstallAddOn.Value;

        public ReactiveCommand<Unit, InstallResult> InstallAddOn { get; }

        private readonly ObservableAsPropertyHelper<bool> _isInstalling;
        public bool IsInstalling => _isInstalling.Value;

        public AddOnViewModel(string? addOnsPath, AddOnMetadata remoteMetadata, LocalAddOnMetadataLoader localAddOnMetadataLoader, IAddOnDownloader addOnDownloader, AddOnBundleInstaller AddOnBundleInstaller)
        {
            AddOnsPath = addOnsPath;
            RemoteMetadata = remoteMetadata;
            _addOnDownloader = addOnDownloader;
            _AddOnBundleInstaller = AddOnBundleInstaller;

            var localMeta = this
                .WhenAnyValue(x => x.RemoteMetadata)
                .SelectMany(async x => await localAddOnMetadataLoader.LoadAddOnMetadata(AddOnsPath, x.Name).ConfigureAwait(false));
            _localMetadata = localMeta.Select(x => x.Item1).ToProperty(this, x => x.LocalMetadata);
            _localMetadataStatus = localMeta.Select(x => x.Item2).ToProperty(this, x => x.LocalMetadataStatus);
            _localMetadataString = this.WhenAnyValue(x => x.LocalMetadata)
                .CombineLatest(this.WhenAnyValue(x => x.LocalMetadataStatus))
                .Select(x =>
                {
                    var (metadata, status) = x;
                    if (status == LocalAddOnMetadataLoader.Status.Found && metadata != null)
                    {
                        return metadata.Version;
                    }
                    else if (status == LocalAddOnMetadataLoader.Status.NotFound)
                    {
                        return "Not Found";
                    }
                    else
                    {
                        return "Error";
                    }
                })
                .ToProperty(this, x => x.LocalMetadataString);

            _canInstallAddOn = localMeta
                .CombineLatest(this.WhenAnyValue(x => x.RemoteMetadata))
                .Select(x =>
                {
                    var ((local, localStatus), remote) = x;
                    var a = !string.IsNullOrEmpty(AddOnsPath) && local?.Version != remote.Version;
                    return a;
                })
                .ToProperty(this, x => x.CanInstallAddOn);

            InstallAddOn = ReactiveCommand.CreateFromTask(DoInstall, this.WhenAnyValue(x => x.CanInstallAddOn));
            _isInstalling = InstallAddOn.IsExecuting.ToProperty(this, x => x.IsInstalling);

            _requiredAction = localMeta
                .CombineLatest(this.WhenAnyValue(x => x.RemoteMetadata), this.WhenAnyValue(x => x.IsInstalling))
                .Select(x =>
                {
                    var ((local, localStatus), remote, isInstalling) = x;

                    if (isInstalling)
                    {
                        return "Installing";
                    }

                    if (localStatus == LocalAddOnMetadataLoader.Status.Found)
                    {
                        if (local?.Version == remote.Version)
                        {
                            return "Up to date";
                        }
                        else
                        {
                            return "Update required";
                        }
                    }
                    else
                    {
                        return "Install required";
                    }
                })
                .ToProperty(this, x => x.RequiredAction);
        }

        private async Task<InstallResult> DoInstall()
        {
            if (AddOnsPath == null)
            {
                Log.Warning("attempted to install add-on but AddOnsPath is null, {AddOnName}", RemoteMetadata.Name);
                return InstallResult.Error;
            }

            var bundle = await _addOnDownloader.GetAddOnBundle(RemoteMetadata).ConfigureAwait(false);
            if (bundle != null)
            {
                _AddOnBundleInstaller.InstallAddOn(AddOnsPath, bundle);
                Log.Information("Add-on {AddOnName} installed successfully", RemoteMetadata.Name);
                return InstallResult.Success;
            }
            else
            {
                Log.Error("Failed to download add-on bundle for {AddOnName}", RemoteMetadata.Name);
                return InstallResult.Error;
            }
        }
    }
}