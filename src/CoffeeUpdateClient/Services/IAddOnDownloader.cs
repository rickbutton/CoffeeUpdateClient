using System.IO;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Services;

public interface IAddOnDownloader
{
    Task<AddOnManifest?> GetLatestManifestAsync();
    Task<AddOnBundle?> GetAddOnBundleAsync(AddOnMetadata metadata);
}