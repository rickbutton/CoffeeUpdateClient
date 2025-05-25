using Path = System.IO.Path;
using System.Text.Json;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Utils;
using Windows.System;

namespace CoffeeUpdateClient.Services;

public class FileSystemConfigService : IConfigService
{
    private readonly IEnv _env;
    private readonly AppDataFolder _appDataFolder;

    public FileSystemConfigService(IEnv env, AppDataFolder appDataFolder)
    {
        _env = env;
        _appDataFolder = appDataFolder;
    }

    private Config CreateDefaultConfig()
    {
        var config = new Config();

        var installPath = InstallLocator.GetWoWInstallPath();
        var addOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(installPath);
        if (addOnsPath != null)
        {
            config.AddOnsPath = addOnsPath;
        }
        else
        {
            config.AddOnsPath = string.Empty;
        }

        return config;
    }

    private async Task<Config> LoadFromPath(string path)
    {
        using var stream = _env.FileSystem.File.OpenRead(path);
        var config = await JsonSerializer.DeserializeAsync<Config>(stream);
        return config!;
    }

    private string GetUserConfigPath()
    {
        return Path.Combine(
            _appDataFolder.GetPath(),
            "config.json"
        );
    }

    private void CreateDefaultConfigIfNotExists()
    {
        _appDataFolder.EnsurePathExists();
        var path = GetUserConfigPath();
        if (!_env.FileSystem.File.Exists(path))
        {
            var config = CreateDefaultConfig();
            using var stream = _env.FileSystem.File.Create(path);
            JsonSerializer.Serialize(stream, config);
        }
    }

    private async Task<Config> LoadFromAppData()
    {
        CreateDefaultConfigIfNotExists();
        return await LoadFromPath(GetUserConfigPath());
    }

    private Config? _instance;

    public async Task<Config> LoadConfigSingleton()
    {
        _instance ??= await LoadFromAppData();
        return _instance;
    }

    public Config Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("ApplicationConfig not loaded. Call LoadConfigSingleton() first.");
            }
            return _instance!;
        }
    }
}