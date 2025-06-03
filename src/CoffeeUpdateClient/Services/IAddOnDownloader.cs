using System.IO;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Services;

public interface IAddOnDownloader
{
    Task<AddOnManifest?> GetLatestManifest();
    Task<AddOnBundle?> GetAddOnBundle(AddOnMetadata metadata);
}