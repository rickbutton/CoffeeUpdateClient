using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Utils;
using CoffeeUpdateClient.Services;
using System.Text.Json;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Tests;

public class FileSystemConfigServiceTest
{
    [Test]
    public async Task CreatesFileAtExpectedLocation()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var service = new FileSystemConfigService(mockEnv, appDataFolder);

        await service.LoadConfigSingleton();

        var expected = @"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient\config.json";
        Assert.That(mockEnv.FileSystem.File.Exists(expected), Is.True);
    }

    [Test]
    public void InstanceThrowsIfLoadConfigSingletonNotCalled()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var service = new FileSystemConfigService(mockEnv, appDataFolder);

        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = service.Instance;
        });
    }

    [Test]
    public async Task LoadConfigSingleton_FileAlreadyExists_DoesNotThrow()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var configPath = @"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient\config.json";

        // Pre-create the config file
        mockEnv.FileSystem.Directory.CreateDirectory(@"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient");
        var existingConfig = new Config { AddOnsPath = @"C:\existing\addons\path" };
        var json = JsonSerializer.Serialize(existingConfig);
        mockEnv.FileSystem.File.WriteAllText(configPath, json);

        var service = new FileSystemConfigService(mockEnv, appDataFolder);

        await service.LoadConfigSingleton();

        Assert.That(mockEnv.FileSystem.File.Exists(configPath), Is.True);
    }

    [Test]
    public async Task LoadConfigSingleton_ExistingFileWithContent_DoesNotOverwriteWithDefaults()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var configPath = @"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient\config.json";
        var existingAddOnsPath = @"C:\custom\existing\addons\path";

        // Pre-create the config file with custom content
        mockEnv.FileSystem.Directory.CreateDirectory(@"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient");
        var existingConfig = new Config { AddOnsPath = existingAddOnsPath };
        var json = JsonSerializer.Serialize(existingConfig);
        mockEnv.FileSystem.File.WriteAllText(configPath, json);

        var service = new FileSystemConfigService(mockEnv, appDataFolder);

        await service.LoadConfigSingleton();
        var loadedContent = mockEnv.FileSystem.File.ReadAllText(configPath);
        var loadedConfig = JsonSerializer.Deserialize<Config>(loadedContent);
        
        Assert.That(loadedConfig, Is.Not.Null);
        Assert.That(loadedConfig!.AddOnsPath, Is.EqualTo(existingAddOnsPath));
        Assert.That(service.Instance.AddOnsPath, Is.EqualTo(existingAddOnsPath));
    }
}