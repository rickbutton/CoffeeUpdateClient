using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Utils;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Tests;

public class LocalAddOnMetadataLoaderTest
{
    [Test]
    public async Task LoadAddOnMetadata_ValidTOCFile_ReturnsAddOnMetadata()
    {
        // Setup test data
        var mockEnv = new MockEnv();
        var mockConfigService = new MockConfigService();
        await mockConfigService.LoadConfigSingleton();
        
        var addOnName = "TestAddOn";
        var addOnVersion = "1.2.3";
        var addOnsPath = mockConfigService.Instance.AddOnsPath;
        var addOnPath = Path.Combine(addOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}.toc");
        
        // Create the directory structure and TOC file
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, $"## Title: Test AddOn\n## Version: {addOnVersion}\n## Interface: 100200");
        
        var loader = new LocalAddOnMetadataLoader(mockEnv, mockConfigService);
        
        // Execute
        var result = await loader.LoadAddOnMetadata(addOnName);
        
        // Verify
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo(addOnName));
        Assert.That(result.Version, Is.EqualTo(addOnVersion));
    }
    
    [Test]
    public async Task LoadAddOnMetadata_TOCFileDoesNotExist_ReturnsNull()
    {
        var mockEnv = new MockEnv();
        var mockConfigService = new MockConfigService();
        await mockConfigService.LoadConfigSingleton();
        
        var addOnName = "NonExistentAddOn";
        
        var loader = new LocalAddOnMetadataLoader(mockEnv, mockConfigService);
        
        var result = await loader.LoadAddOnMetadata(addOnName);
        
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public async Task LoadAddOnMetadata_TOCFileExistsButNoVersion_ReturnsNull()
    {
        // Setup test data
        var mockEnv = new MockEnv();
        var mockConfigService = new MockConfigService();
        await mockConfigService.LoadConfigSingleton();
        
        var addOnName = "AddOnWithoutVersion";
        var addOnsPath = mockConfigService.Instance.AddOnsPath;
        var addOnPath = Path.Combine(addOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}.toc");
        
        // Create the directory structure and TOC file without version
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, "## Title: AddOn Without Version\n## Interface: 100200");
        
        var loader = new LocalAddOnMetadataLoader(mockEnv, mockConfigService);
        
        // Execute
        var result = await loader.LoadAddOnMetadata(addOnName);
        
        // Verify
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public async Task LoadAddOnMetadata_TOCFileWithEmptyVersion_ReturnsNull()
    {
        // Setup test data
        var mockEnv = new MockEnv();
        var mockConfigService = new MockConfigService();
        await mockConfigService.LoadConfigSingleton();
        
        var addOnName = "AddOnWithEmptyVersion";
        var addOnsPath = mockConfigService.Instance.AddOnsPath;
        var addOnPath = Path.Combine(addOnsPath, addOnName);
        var tocPath = Path.Combine(addOnPath, $"{addOnName}.toc");
        
        // Create the directory structure and TOC file with empty version
        mockEnv.FileSystem.Directory.CreateDirectory(addOnPath);
        mockEnv.FileSystem.File.WriteAllText(tocPath, "## Title: AddOn With Empty Version\n## Version: \n## Interface: 100200");
        
        var loader = new LocalAddOnMetadataLoader(mockEnv, mockConfigService);
        
        // Execute
        var result = await loader.LoadAddOnMetadata(addOnName);
        
        // Verify
        Assert.That(result, Is.Null);
    }
}
