using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Utils;
using CoffeeUpdateClient.Models;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CoffeeUpdateClient.Tests;

public class LocalAddOnMetadataLoaderTest
{
    private const string AddOnsPath = @"C:\World of Warcraft\_retail_\Interface\AddOns";

    private static LocalAddOnMetadataLoader CreateLoader(MockEnv mockEnv)
    {
        var config = new Config { AddOnsPath = AddOnsPath };
        return new LocalAddOnMetadataLoader(mockEnv, config);
    }

    [Test]
    public async Task LoadAddOnMetadata_StandardTOCFile_ReturnsAddOnMetadataAsync()
    {
        var mockEnv = new MockEnv();

        var addOnName = "TestAddOn";
        var addOnVersion = "1.2.3";
        var addOnPath = Path.Combine(AddOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}.toc");

        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, $"## Title: Test AddOn\n## Version: {addOnVersion}\n## Interface: 100200");

        var loader = CreateLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync(addOnName);

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_MainlineTOCFile_ReturnsAddOnMetadataAsync()
    {
        var mockEnv = new MockEnv();

        var addOnName = "TestAddOn";
        var addOnVersion = "2.0.0";
        var addOnPath = Path.Combine(AddOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}_Mainline.toc");

        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, $"## Title: Test AddOn Mainline\n## Version: {addOnVersion}\n## Interface: 100200");

        var loader = CreateLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync(addOnName);

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_StandardWoWTOCFile_ReturnsAddOnMetadataAsync()
    {
        var mockEnv = new MockEnv();

        var addOnName = "TestAddOn";
        var addOnVersion = "3.0.0";
        var addOnPath = Path.Combine(AddOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}_Standard.toc");

        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, $"## Title: Test AddOn Standard\n## Version: {addOnVersion}\n## Interface: 11404");

        var loader = CreateLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync(addOnName);

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_FallbackToAlternativeTOCFiles_ReturnsAddOnMetadataAsync()
    {
        var mockEnv = new MockEnv();

        var addOnName = "TestAddOn";
        var addOnVersion = "2.0.0";
        var addOnPath = Path.Combine(AddOnsPath, addOnName);

        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}_Mainline.toc"),
            $"## Title: Test AddOn Mainline\n## Version: {addOnVersion}\n## Interface: 100200");

        var loader = CreateLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync(addOnName);

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_TOCFileDoesNotExist_ReturnsNullAsync()
    {
        var mockEnv = new MockEnv();
        var loader = CreateLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync("NonExistentAddOn");

        Assert.That(metadata, Is.Null);
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.NotFound));
    }

    [Test]
    public async Task LoadAddOnMetadata_TOCFilesExistButNoVersion_ReturnsNullAsync()
    {
        var mockEnv = new MockEnv();

        var addOnName = "AddOnWithoutVersion";
        var addOnPath = Path.Combine(AddOnsPath, addOnName);

        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}.toc"),
            "## Title: AddOn Without Version\n## Interface: 100200");
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}_Mainline.toc"),
            "## Title: AddOn Without Version Mainline\n## Interface: 100200");

        var loader = CreateLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync(addOnName);

        Assert.That(metadata, Is.Null);
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.Error));
    }

    [Test]
    public async Task LoadAddOnMetadata_EmptyAddOnsPath_ReturnsNotFoundAsync()
    {
        var mockEnv = new MockEnv();
        var config = new Config { AddOnsPath = "" };
        var loader = new LocalAddOnMetadataLoader(mockEnv, config);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync("AnyAddOn");

        Assert.That(metadata, Is.Null);
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.NotFound));
    }

    [Test]
    public async Task LoadAddOnMetadata_PrioritizesDefaultTOCOverAlternativesAsync()
    {
        var mockEnv = new MockEnv();

        var addOnName = "PriorityAddOn";
        var defaultVersion = "1.0.0";
        var mainlineVersion = "2.0.0";
        var standardVersion = "3.0.0";
        var addOnPath = Path.Combine(AddOnsPath, addOnName);

        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}.toc"),
            $"## Title: Priority AddOn\n## Version: {defaultVersion}\n## Interface: 100200");
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}_Mainline.toc"),
            $"## Title: Priority AddOn Mainline\n## Version: {mainlineVersion}\n## Interface: 100200");
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}_Standard.toc"),
            $"## Title: Priority AddOn Standard\n## Version: {standardVersion}\n## Interface: 11404");

        var loader = CreateLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadataAsync(addOnName);

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(defaultVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.Status.Found));
    }
}
