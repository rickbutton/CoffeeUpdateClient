using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using CoffeeUpdater.Tests.Mocks;

namespace CoffeeUpdater.Tests;

public class LocalFileAddOnDownloaderTest
{
    private MockEnv _env = null!;
    private LocalFileAddOnDownloader _downloader = null!;
    private const string BaseDir = @"C:\local-manifest";

    [SetUp]
    public void SetUp()
    {
        _env = new MockEnv();
        _env.FileSystem.Directory.CreateDirectory(BaseDir);
        _downloader = new LocalFileAddOnDownloader(_env.FileSystem, BaseDir);
    }

    private byte[] CreateZipBytes(string rootFolder, string fileName, string content)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry($"{rootFolder}/{fileName}");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(content);
        }
        return ms.ToArray();
    }

    [Test]
    public async Task GetLatestManifestAsync_ValidManifest_ReturnsManifest()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [new AddOnMetadata { Name = "MyAddOn", Version = "1.0.0" }]
        };
        _env.MockFileSystem.AddFile(
            @"C:\local-manifest\manifest.json",
            new MockFileData(JsonSerializer.Serialize(manifest))
        );

        var result = await _downloader.GetLatestManifestAsync();

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Updated));
        Assert.That(result.Manifest, Is.Not.Null);
        Assert.That(result.Manifest!.AddOns, Has.Count.EqualTo(1));
        Assert.That(result.Manifest.AddOns[0].Name, Is.EqualTo("MyAddOn"));
        Assert.That(result.Manifest.AddOns[0].Version, Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task GetLatestManifestAsync_MultipleAddOns_ReturnsAll()
    {
        var manifest = new AddOnManifest
        {
            AddOns =
            [
                new AddOnMetadata { Name = "AddOnA", Version = "1.0.0" },
                new AddOnMetadata { Name = "AddOnB", Version = "2.3.0" },
            ]
        };
        _env.MockFileSystem.AddFile(
            @"C:\local-manifest\manifest.json",
            new MockFileData(JsonSerializer.Serialize(manifest))
        );

        var result = await _downloader.GetLatestManifestAsync();

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Updated));
        Assert.That(result.Manifest!.AddOns, Has.Count.EqualTo(2));
        Assert.That(result.Manifest.AddOns[1].Name, Is.EqualTo("AddOnB"));
    }

    [Test]
    public async Task GetLatestManifestAsync_MissingFile_ReturnsFailed()
    {
        var result = await _downloader.GetLatestManifestAsync();

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Failed));
        Assert.That(result.Manifest, Is.Null);
    }

    [Test]
    public async Task GetAddOnBundleAsync_ValidZip_ReturnsBundleWithCorrectMetadata()
    {
        var metadata = new AddOnMetadata { Name = "MyAddOn", Version = "1.0.0" };
        _env.MockFileSystem.AddFile(
            @"C:\local-manifest\addons\MyAddOn-1.0.0.zip",
            new MockFileData(CreateZipBytes("MyAddOn", "MyAddOn.lua", "-- lua"))
        );

        var result = await _downloader.GetAddOnBundleAsync(metadata);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Metadata.Name, Is.EqualTo("MyAddOn"));
        Assert.That(result.Metadata.Version, Is.EqualTo("1.0.0"));
        result.Data.Dispose();
    }

    [Test]
    public async Task GetAddOnBundleAsync_ValidZip_ReturnsReadableStream()
    {
        var zipBytes = CreateZipBytes("MyAddOn", "MyAddOn.lua", "-- lua");
        _env.MockFileSystem.AddFile(
            @"C:\local-manifest\addons\MyAddOn-1.0.0.zip",
            new MockFileData(zipBytes)
        );

        var result = await _downloader.GetAddOnBundleAsync(new AddOnMetadata { Name = "MyAddOn", Version = "1.0.0" });

        Assert.That(result!.Data.Length, Is.EqualTo(zipBytes.Length));
        result.Data.Dispose();
    }

    [Test]
    public async Task GetAddOnBundleAsync_MissingFile_ReturnsNull()
    {
        var result = await _downloader.GetAddOnBundleAsync(new AddOnMetadata { Name = "Missing", Version = "9.9.9" });

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetLatestManifestAsync_JsonNull_ReturnsFailed()
    {
        _env.MockFileSystem.AddFile(@"C:\local-manifest\manifest.json", new MockFileData("null"));

        var result = await _downloader.GetLatestManifestAsync();

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Failed));
        Assert.That(result.Manifest, Is.Null);
    }
}
