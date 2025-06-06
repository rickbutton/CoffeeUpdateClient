using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Utils;
using CoffeeUpdateClient.Services;
using System.Text.Json;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Tests;

public class FileSystemConfigServiceTest : ConfigTestBase
{
    [Test]
    public async Task CreatesFileAtExpectedLocationAsync()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var wowLocator = new MockWowLocator(@"C:\World of Warcraft");

        var service = new FileSystemConfigService(mockEnv, appDataFolder, wowLocator);
        await service.GetConfigAsync();

        var expected = @"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient\config.json";
        Assert.That(mockEnv.FileSystem.File.Exists(expected), Is.True);

        var loadedContent = mockEnv.FileSystem.File.ReadAllText(expected);
        var loadedConfig = JsonSerializer.Deserialize<Config>(loadedContent);

        Assert.That(loadedConfig?.AddOnsPath, Is.EqualTo(@"C:\World of Warcraft\_retail_\Interface\AddOns"));
    }

    [Test]
    public async Task LoadConfigSingleton_FileAlreadyExists_DoesNotThrowAsync()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var wowLocator = new MockWowLocator();
        var configPath = @"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient\config.json";

        // Pre-create the config file
        mockEnv.FileSystem.Directory.CreateDirectory(@"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient");
        var existingConfig = new Config { AddOnsPath = @"C:\existing\addons\path" };
        var json = JsonSerializer.Serialize(existingConfig);
        mockEnv.FileSystem.File.WriteAllText(configPath, json);

        var service = new FileSystemConfigService(mockEnv, appDataFolder, wowLocator);

        await service.GetConfigAsync();

        Assert.That(mockEnv.FileSystem.File.Exists(configPath), Is.True);
    }

    [Test]
    public async Task LoadConfigSingleton_ExistingFileWithContent_DoesNotOverwriteWithDefaultsAsync()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var wowLocator = new MockWowLocator();
        var configPath = @"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient\config.json";
        var existingAddOnsPath = @"C:\custom\existing\addons\path";

        // Pre-create the config file with custom content
        mockEnv.FileSystem.Directory.CreateDirectory(@"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient");
        var existingConfig = new Config { AddOnsPath = existingAddOnsPath };
        var json = JsonSerializer.Serialize(existingConfig);
        mockEnv.FileSystem.File.WriteAllText(configPath, json);

        var service = new FileSystemConfigService(mockEnv, appDataFolder, wowLocator);

        var config = await service.GetConfigAsync();
        var loadedContent = mockEnv.FileSystem.File.ReadAllText(configPath);
        var loadedConfig = JsonSerializer.Deserialize<Config>(loadedContent);

        Assert.That(loadedConfig, Is.Not.Null);
        Assert.That(loadedConfig!.AddOnsPath, Is.EqualTo(existingAddOnsPath));
        Assert.That(config.AddOnsPath, Is.EqualTo(existingAddOnsPath));
    }

    [Test]
    public async Task LoadConfigSingleton_FileAlreadyExists_AutomaticallyWritesChangesAsync()
    {
        var mockEnv = new MockEnv();
        var appDataFolder = new AppDataFolder(mockEnv);
        var wowLocator = new MockWowLocator();
        var configPath = @"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient\config.json";

        mockEnv.FileSystem.Directory.CreateDirectory(@"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient");
        var existingConfig = new Config { AddOnsPath = @"C:\existing\addons\path" };
        var json = JsonSerializer.Serialize(existingConfig);
        mockEnv.FileSystem.File.WriteAllText(configPath, json);

        var service = new FileSystemConfigService(mockEnv, appDataFolder, wowLocator);

        var config = await service.GetConfigAsync();
        Assert.That(config, Is.Not.Null);
        Assert.That(config!.AddOnsPath, Is.EqualTo(existingConfig.AddOnsPath));

        config.AddOnsPath = @"C:\new\addons\path";

        var loadedContent = mockEnv.FileSystem.File.ReadAllText(configPath);
        var loadedConfig = JsonSerializer.Deserialize<Config>(loadedContent);

        Assert.That(loadedConfig?.AddOnsPath, Is.EqualTo(@"C:\new\addons\path"));
    }
}