using System.IO;
using System.IO.Compression;
using System.Text;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using Serilog;

namespace CoffeeUpdateClient.Tests.Mocks;

public class MockAddOnDownloader : IAddOnDownloader
{
    private AddOnManifest? _manifest;
    private readonly Dictionary<string, AddOnBundle?> _bundles = new();
    private bool _shouldReturnNullManifest = false;
    private bool _shouldReturnNullBundle = false;

    public void SetManifest(AddOnManifest? manifest)
    {
        _manifest = manifest;
    }

    public void SetShouldReturnNullManifest(bool shouldReturnNull)
    {
        _shouldReturnNullManifest = shouldReturnNull;
    }

    public void SetShouldReturnNullBundle(bool shouldReturnNull)
    {
        _shouldReturnNullBundle = shouldReturnNull;
    }

    public void AddBundle(string addOnName, string version, string[]? files = null)
    {
        var key = $"{addOnName}-{version}";
        var metadata = new AddOnMetadata { Name = addOnName, Version = version };

        if (files != null)
        {
            var bundle = CreateBundle(metadata, files);
            _bundles[key] = bundle;
        }
        else
        {
            _bundles[key] = null;
        }
    }

    public async Task<AddOnManifest?> GetLatestManifestAsync()
    {
        await Task.Delay(1); // Simulate async operation
        return _shouldReturnNullManifest ? null : _manifest;
    }

    public async Task<AddOnBundle?> GetAddOnBundleAsync(AddOnMetadata metadata)
    {
        await Task.Delay(1); // Simulate async operation

        if (_shouldReturnNullBundle)
            return null;

        var key = $"{metadata.Name}-{metadata.Version}";
        return _bundles.TryGetValue(key, out var bundle) ? bundle : null;
    }

    private AddOnBundle CreateBundle(AddOnMetadata metadata, string[] fileNames)
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var fileName in fileNames)
            {
                var entry = archive.CreateEntry($"{metadata.Name}/{fileName}");
                using (var entryStream = entry.Open())
                using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    streamWriter.Write($"Content of {fileName}");
                }
            }
        }
        memoryStream.Seek(0, SeekOrigin.Begin);

        return new AddOnBundle(metadata, memoryStream);
    }
}
