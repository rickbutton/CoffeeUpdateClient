using CoffeeUpdater.Models;

namespace CoffeeUpdater.Services;

public interface IAddOnDownloader
{
    Task<ManifestResult> GetLatestManifestAsync();
    Task<AddOnBundle?> GetAddOnBundleAsync(AddOnMetadata metadata);
}
