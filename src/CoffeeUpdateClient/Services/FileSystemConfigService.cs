using Path = System.IO.Path;
using System.Text.Json;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Utils;
using Windows.System;
using Serilog;
using System.IO;

namespace CoffeeUpdateClient.Services;

public class FileSystemConfigService : IConfigService
{
    private readonly IEnv _env;
    private readonly AppDataFolder _appDataFolder;
    private readonly IWoWLocator _wowLocator;

    public FileSystemConfigService(IEnv env, AppDataFolder appDataFolder, IWoWLocator wowLocator)
    {
        _env = env;
        _appDataFolder = appDataFolder;
        _wowLocator = wowLocator;
    }

    private Config CreateDefaultConfig()
    {
        var config = new Config();

        var installPath = _wowLocator.GetWoWInstallPath();
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

    private async Task<Config> LoadFromPathAsync(string path)
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
        var path = GetUserConfigPath();
        if (!_env.FileSystem.File.Exists(path))
        {
            var config = CreateDefaultConfig();
            SaveConfig(config);
        }
    }

    public async Task<Config> GetConfigAsync()
    {
        CreateDefaultConfigIfNotExists();
        var config = await LoadFromPathAsync(GetUserConfigPath());
        config.ConfigUpdated += OnConfigUpdated;
        return config;
    }

    private void OnConfigUpdated(Config config)
    {
        SaveConfig(config);
        Log.Information("configuration saved", config);
    }

    private void SaveConfig(Config config)
    {
        _appDataFolder.EnsurePathExists();
        var path = GetUserConfigPath();
        using var stream = _env.FileSystem.File.Open(path, FileMode.Create);
        JsonSerializer.Serialize(stream, config);
    }
}