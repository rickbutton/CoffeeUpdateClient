using System.IO;
using System.Text.Json;
using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Utils;

class ConfigLoader
{
    private static Config CreateDefaultConfig()
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

    private static async Task<Config> LoadFromPath(string path)
    {
        using FileStream stream = File.OpenRead(path);
        var config = await JsonSerializer.DeserializeAsync<Config>(stream);
        return config!;
    }

    private static string GetUserConfigPath()
    {
        return Path.Combine(
            AppDataFolder.GetPath(),
            "config.json"
        );
    }

    private static void CreateDefaultConfigIfNotExists()
    {
        AppDataFolder.EnsurePathExists();
        var path = GetUserConfigPath();
        if (!File.Exists(path))
        {
            var config = CreateDefaultConfig();
            using FileStream stream = File.Create(path);
            JsonSerializer.Serialize(stream, config);
        }
    }

    private static async Task<Config> LoadFromAppData()
    {
        CreateDefaultConfigIfNotExists();
        return await LoadFromPath(GetUserConfigPath());
    }

    private static Config? _instance;

    public static async Task LoadConfigSingleton()
    {
        _instance ??= await LoadFromAppData();
    }

    public static Config Instance
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