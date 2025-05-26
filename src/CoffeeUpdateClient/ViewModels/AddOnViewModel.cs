using System.Reactive.Linq;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Utils;
using ReactiveUI;

namespace CoffeeUpdateClient.ViewModels
{
    public class AddOnViewModel : ReactiveObject
    {
        public string? AddOnsPath { get; }
        public AddOnMetadata RemoteMetadata { get; }

        private readonly ObservableAsPropertyHelper<AddOnMetadata?> _localMetadata;
        public AddOnMetadata? LocalMetadata => _localMetadata.Value;
        private readonly ObservableAsPropertyHelper<LocalAddOnMetadataLoader.LocalAddOnMetadataStatus> _localMetadataStatus;
        public LocalAddOnMetadataLoader.LocalAddOnMetadataStatus LocalMetadataStatus => _localMetadataStatus.Value;

        public readonly ObservableAsPropertyHelper<string> _localMetadataString;
        public string LocalMetadataString => _localMetadataString.Value;

        public readonly ObservableAsPropertyHelper<string> _requiredAction;
        public string RequiredAction => _requiredAction.Value;

        public AddOnViewModel(string? addOnsPath, AddOnMetadata remoteMetadata, LocalAddOnMetadataLoader localAddOnMetadataLoader)
        {
            AddOnsPath = addOnsPath;
            RemoteMetadata = remoteMetadata;

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
                    if (status == LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Found && metadata != null)
                    {
                        return metadata.Version;
                    }
                    else if (status == LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.NotFound)
                    {
                        return "Not Found";
                    }
                    else
                    {
                        return "Error";
                    }
                })
                .ToProperty(this, x => x.LocalMetadataString);
            _requiredAction = localMeta
                .CombineLatest(this.WhenAnyValue(x => x.RemoteMetadata))
                .Select(x =>
                {
                    var ((local, localStatus), remote) = x;

                    if (localStatus == LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Found)
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
    }
}