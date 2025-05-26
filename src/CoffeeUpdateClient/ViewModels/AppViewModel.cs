using System.Reactive;
using System.Reactive.Linq;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Utils;
using Microsoft.Win32;
using ReactiveUI;
using Serilog;

namespace CoffeeUpdateClient.ViewModels;

public class AppViewModel : ReactiveObject
{
    public enum AddOnsPathStateEnum
    {
        Invalid,
        Valid,
        NotSet,
    };

    private string _addOnsPath;
    public string AddOnsPath
    {
        get => _addOnsPath;
        set => this.RaiseAndSetIfChanged(ref _addOnsPath, value);
    }

    private readonly ObservableAsPropertyHelper<string?> _normalizedAddOnsPath;
    public string? NormalizedAddOnsPath => _normalizedAddOnsPath.Value;

    private bool _userHasSelectedPath;
    public bool UserHasSelectedPath
    {
        get => _userHasSelectedPath;
        set => this.RaiseAndSetIfChanged(ref _userHasSelectedPath, value);
    }

    private readonly ObservableAsPropertyHelper<AddOnsPathStateEnum> _addOnsPathState;
    public AddOnsPathStateEnum AddOnsPathState => _addOnsPathState.Value;

    private readonly ObservableAsPropertyHelper<AddOnManifest?> _remoteAddOnManifest;
    public AddOnManifest? RemoteAddOnManifest => _remoteAddOnManifest.Value;

    private readonly ObservableAsPropertyHelper<bool> _remoteAddOnManifestIsAvailable;
    public bool RemoteAddOnManifestIsAvailable => _remoteAddOnManifestIsAvailable.Value;

    private readonly ObservableAsPropertyHelper<IEnumerable<AddOnViewModel>> _addOns;
    public IEnumerable<AddOnViewModel> AddOns => _addOns.Value;

    public ReactiveCommand<Unit, string?> Browse { get; }

    public AppViewModel(IConfigService configService, IAddOnDownloader addOnDownloader, LocalAddOnMetadataLoader localAddOnMetadataLoader)
    {
        _addOnsPath = configService.Instance.AddOnsPath ?? string.Empty;
        _normalizedAddOnsPath = this
            .WhenAnyValue(x => x.AddOnsPath)
            .Select(AddOnPathResolver.NormalizeAddOnsDirectory)
            .ToProperty(this, x => x.NormalizedAddOnsPath);
        _userHasSelectedPath = false;
        _addOnsPathState = this
            .WhenAnyValue(x => x.AddOnsPath)
            .CombineLatest(this.WhenAnyValue(x => x.NormalizedAddOnsPath))
            .Select((x) =>
            {
                var (path, normalizedPath) = x;
                Log.Debug("AddOnsPathState: {Path}, {NormalizedPath}", path, normalizedPath);

                if (string.IsNullOrEmpty(path))
                {
                    return AddOnsPathStateEnum.NotSet;
                }
                else if (string.IsNullOrEmpty(normalizedPath))
                {
                    return AddOnsPathStateEnum.Invalid;
                }
                else
                {
                    return AddOnsPathStateEnum.Valid;
                }
            })
            .ToProperty(this, x => x.AddOnsPathState);
        _remoteAddOnManifest = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(5))
            .SelectMany(async x =>
            {
                Log.Information("Fetching addon manifest");
                var manifest = await addOnDownloader.GetLatestManifest().ConfigureAwait(false);
                Log.Information("addon manifest fetched: {Manifest}", manifest);
                return manifest;
            })
            .ToProperty(this, x => x.RemoteAddOnManifest);
        _remoteAddOnManifestIsAvailable = this
            .WhenAnyValue(x => x.RemoteAddOnManifest)
            .Select(x => x != null)
            .ToProperty(this, x => x.RemoteAddOnManifestIsAvailable);
        _addOns = this
            .WhenAnyValue(x => x.RemoteAddOnManifest)
            .CombineLatest(this.WhenAnyValue(x => x.NormalizedAddOnsPath))
            .Select(x =>
            {
                var (manifest, normalizedPath) = x;
                return manifest?.AddOns != null ? manifest.AddOns.Select(y => new AddOnViewModel(normalizedPath, y, localAddOnMetadataLoader)) : Enumerable.Empty<AddOnViewModel>();
            })
            .ToProperty(this, x => x.AddOns);

        Browse = ReactiveCommand.Create<Unit, string?>((_) =>
        {
            var dialog = new OpenFolderDialog();
            if (!string.IsNullOrEmpty(NormalizedAddOnsPath))
            {
                dialog.InitialDirectory = NormalizedAddOnsPath;
            }

            if (dialog.ShowDialog() == true)
            {
                AddOnsPath = dialog.FolderName;
                UserHasSelectedPath = true;
                Log.Information("User selected path: {Path}", AddOnsPath);
                return AddOnsPath;
            }
            else
            {
                return null;
            }
        });
    }
}