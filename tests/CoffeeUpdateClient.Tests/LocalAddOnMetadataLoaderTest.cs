using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Utils;
using CoffeeUpdateClient.Models;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CoffeeUpdateClient.Tests;

public class LocalAddOnMetadataLoaderTest
{
    [Test]
    public async Task LoadAddOnMetadata_StandardTOCFile_ReturnsAddOnMetadata()
    {
        // Setup test data
        var mockEnv = new MockEnv();

        var addOnName = "TestAddOn";
        var addOnVersion = "1.2.3";
        var addOnsPath = "C:\\World of Warcraft\\_retail_\\Interface\\AddOns";
        var addOnPath = Path.Combine(addOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}.toc");

        // Create the directory structure and TOC file
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, $"## Title: Test AddOn\n## Version: {addOnVersion}\n## Interface: 100200");

        var loader = new LocalAddOnMetadataLoader(mockEnv);

        // Execute
        var (metadata, status) = await loader.LoadAddOnMetadata(addOnsPath, addOnName);

        // Verify
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_MainlineTOCFile_ReturnsAddOnMetadata()
    {
        // Setup test data
        var mockEnv = new MockEnv();

        var addOnName = "TestAddOn";
        var addOnVersion = "2.0.0";
        var addOnsPath = "C:\\World of Warcraft\\_retail_\\Interface\\AddOns";
        var addOnPath = Path.Combine(addOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}_Mainline.toc");

        // Create the directory structure and TOC file
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, $"## Title: Test AddOn Mainline\n## Version: {addOnVersion}\n## Interface: 100200");

        var loader = new LocalAddOnMetadataLoader(mockEnv);

        // Execute
        var (metadata, status) = await loader.LoadAddOnMetadata(addOnsPath, addOnName);

        // Verify
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_StandardWoWTOCFile_ReturnsAddOnMetadata()
    {
        // Setup test data
        var mockEnv = new MockEnv();
        var mockConfigService = new MockConfigService();
        await mockConfigService.LoadConfigSingleton();

        var addOnName = "TestAddOn";
        var addOnVersion = "3.0.0";
        var addOnsPath = "C:\\World of Warcraft\\_retail_\\Interface\\AddOns";
        var addOnPath = Path.Combine(addOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}_Standard.toc");

        // Create the directory structure and TOC file
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, $"## Title: Test AddOn Standard\n## Version: {addOnVersion}\n## Interface: 11404");

        var loader = new LocalAddOnMetadataLoader(mockEnv);

        // Execute
        var (metadata, status) = await loader.LoadAddOnMetadata(addOnsPath, addOnName);

        // Verify
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_FallbackToAlternativeTOCFiles_ReturnsAddOnMetadata()
    {
        // Setup test data
        var mockEnv = new MockEnv();

        var addOnName = "TestAddOn";
        var addOnVersion = "2.0.0";
        var addOnsPath = "C:\\World of Warcraft\\_retail_\\Interface\\AddOns";
        var addOnPath = Path.Combine(addOnsPath, addOnName);

        // Create the directory structure but skip the default TOC
        // Instead create the Mainline version
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}_Mainline.toc"),
            $"## Title: Test AddOn Mainline\n## Version: {addOnVersion}\n## Interface: 100200");

        var loader = new LocalAddOnMetadataLoader(mockEnv);

        // Execute
        var (metadata, status) = await loader.LoadAddOnMetadata(addOnsPath, addOnName);

        // Verify
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(addOnVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Found));
    }

    [Test]
    public async Task LoadAddOnMetadata_TOCFileDoesNotExist_ReturnsNull()
    {
        var mockEnv = new MockEnv();

        var addOnsPath = "C:\\World of Warcraft\\_retail_\\Interface\\AddOns";
        var addOnName = "NonExistentAddOn";

        var loader = new LocalAddOnMetadataLoader(mockEnv);

        var (metadata, status) = await loader.LoadAddOnMetadata(addOnsPath, addOnName);

        Assert.That(metadata, Is.Null);
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.NotFound));
    }

    [Test]
    public async Task LoadAddOnMetadata_TOCFilesExistButNoVersion_ReturnsNull()
    {
        // Setup test data
        var mockEnv = new MockEnv();

        var addOnName = "AddOnWithoutVersion";
        var addOnsPath = "C:\\World of Warcraft\\_retail_\\Interface\\AddOns";
        var addOnPath = Path.Combine(addOnsPath, addOnName);

        // Create the directory structure and multiple TOC files without version
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}.toc"),
            "## Title: AddOn Without Version\n## Interface: 100200");
        mockEnv.FileSystem.File.WriteAllText(
            Path.Combine(addOnPath, $"{addOnName}_Mainline.toc"),
            "## Title: AddOn Without Version Mainline\n## Interface: 100200");

        var loader = new LocalAddOnMetadataLoader(mockEnv);

        // Execute
        var (metadata, status) = await loader.LoadAddOnMetadata(addOnsPath, addOnName);

        // Verify
        Assert.That(metadata, Is.Null);
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Error));
    }

    [Test]
    public async Task LoadAddOnMetadata_PrioritizesDefaultTOCOverAlternatives()
    {
        // Setup test data
        var mockEnv = new MockEnv();

        var addOnName = "PriorityAddOn";
        var defaultVersion = "1.0.0";
        var mainlineVersion = "2.0.0";
        var standardVersion = "3.0.0";
        var addOnsPath = "C:\\World of Warcraft\\_retail_\\Interface\\AddOns";
        var addOnPath = Path.Combine(addOnsPath, addOnName);

        // Create the directory structure and multiple TOC files with different versions
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

        var loader = new LocalAddOnMetadataLoader(mockEnv);

        // Execute
        var (metadata, status) = await loader.LoadAddOnMetadata(addOnsPath, addOnName);

        // Verify - should use the default TOC file (first in the list)
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo(addOnName));
        Assert.That(metadata.Version, Is.EqualTo(defaultVersion));
        Assert.That(status, Is.EqualTo(LocalAddOnMetadataLoader.LocalAddOnMetadataStatus.Found));
    }
}
