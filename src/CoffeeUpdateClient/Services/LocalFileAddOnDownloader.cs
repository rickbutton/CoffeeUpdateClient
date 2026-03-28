using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using CoffeeUpdateClient.Models;
using Serilog;

namespace CoffeeUpdateClient.Services;

public class LocalFileAddOnDownloader : IAddOnDownloader
{
    private readonly string _baseDirectory;
    private readonly IFileSystem _fileSystem;

    public LocalFileAddOnDownloader(IFileSystem fileSystem, string baseDirectory)
    {
        _fileSystem = fileSystem;
        _baseDirectory = baseDirectory;
    }

    public Task<AddOnManifest?> GetLatestManifestAsync()
    {
        var manifestPath = _fileSystem.Path.Combine(_baseDirectory, "manifest.json");
        Log.Information("Loading local manifest from {Path}", manifestPath);

        if (!_fileSystem.File.Exists(manifestPath))
        {
            Log.Error("Local manifest not found at {Path}", manifestPath);
            return Task.FromResult<AddOnManifest?>(null);
        }

        var content = _fileSystem.File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<AddOnManifest>(content);
        return Task.FromResult(manifest);
    }

    public Task<AddOnBundle?> GetAddOnBundleAsync(AddOnMetadata metadata)
    {
        var zipPath = _fileSystem.Path.Combine(_baseDirectory, "addons", $"{metadata.Name}-{metadata.Version}.zip");
        Log.Information("Loading local addon bundle from {Path}", zipPath);

        if (!_fileSystem.File.Exists(zipPath))
        {
            Log.Error("Local addon bundle not found at {Path}", zipPath);
            return Task.FromResult<AddOnBundle?>(null);
        }

        Stream data = _fileSystem.File.OpenRead(zipPath);
        return Task.FromResult<AddOnBundle?>(new AddOnBundle(metadata, data));
    }
}
