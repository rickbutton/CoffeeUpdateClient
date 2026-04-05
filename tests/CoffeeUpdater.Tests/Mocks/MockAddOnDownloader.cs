using System.IO;
using System.IO.Compression;
using System.Text;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using Serilog;

namespace CoffeeUpdater.Tests.Mocks;

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

    public void AddBundle(string addOnName, string version, string[]? files = null, string? rootFolderName = null)
    {
        var key = $"{addOnName}-{version}";
        var metadata = new AddOnMetadata { Name = addOnName, Version = version };

        if (files != null)
        {
            var bundle = CreateBundle(metadata, files, rootFolderName);
            _bundles[key] = bundle;
        }
        else
        {
            _bundles[key] = null;
        }
    }

    public void AddMultiFolderBundle(string addonName, string version, Dictionary<string, string[]> folderFiles)
    {
        var key = $"{addonName}-{version}";
        var metadata = new AddOnMetadata { Name = addonName, Version = version };
        _bundles[key] = CreateMultiFolderBundle(metadata, folderFiles);
    }

    public async Task<ManifestResult> GetLatestManifestAsync()
    {
        await Task.Delay(1); // Simulate async operation
        if (_shouldReturnNullManifest || _manifest == null)
            return ManifestResult.Failed();
        return ManifestResult.Updated(_manifest);
    }

    public async Task<AddOnBundle?> GetAddOnBundleAsync(AddOnMetadata metadata)
    {
        await Task.Delay(1); // Simulate async operation

        if (_shouldReturnNullBundle)
            return null;

        var key = $"{metadata.Name}-{metadata.Version}";
        return _bundles.TryGetValue(key, out var bundle) ? bundle : null;
    }

    private AddOnBundle CreateBundle(AddOnMetadata metadata, string[] fileNames, string? rootFolderName = null)
    {
        var root = rootFolderName ?? metadata.Name;
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var fileName in fileNames)
            {
                var entry = archive.CreateEntry($"{root}/{fileName}");
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

    private AddOnBundle CreateMultiFolderBundle(AddOnMetadata metadata, Dictionary<string, string[]> folderFiles)
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (folder, files) in folderFiles)
            {
                foreach (var fileName in files)
                {
                    var entry = archive.CreateEntry($"{folder}/{fileName}");
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write($"Content of {folder}/{fileName}");
                }
            }
        }
        memoryStream.Seek(0, SeekOrigin.Begin);
        return new AddOnBundle(metadata, memoryStream);
    }
}
