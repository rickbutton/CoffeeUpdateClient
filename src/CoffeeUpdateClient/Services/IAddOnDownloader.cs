using System.IO;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Services;

public interface IAddOnDownloader
{
    Task<AddOnManifest?> GetLatestManifest();
    Task<Stream?> GetAddOn(AddOnMetadata metadata);
}