using Path = System.IO.Path;
using System.Text.Json;
using System.IO;
using CoffeeUpdater.Models;
using CoffeeUpdater.Utils;
using Serilog;

namespace CoffeeUpdater.Services;

public class FileSystemConfigService : IConfigService
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

    private readonly IEnv _env;
    private readonly AppDataFolder _appDataFolder;
    private readonly IWoWLocator _wowLocator;

    private readonly object _saveLock = new();
    private CancellationTokenSource? _debounceCts;
    private Config? _pendingConfig;

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
        config.AddOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(installPath) ?? string.Empty;

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
            SaveConfigImmediately(config);
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
        lock (_saveLock)
        {
            _pendingConfig = config;
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
        }

        var cts = _debounceCts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cts.Token);
                FlushPendingSave();
            }
            catch (OperationCanceledException)
            {
                // Debounce reset — a newer save is pending
            }
        });
    }

    private void FlushPendingSave()
    {
        Config? config;
        lock (_saveLock)
        {
            config = _pendingConfig;
            _pendingConfig = null;
        }
        if (config != null)
        {
            SaveConfigImmediately(config);
            Log.Information("configuration saved");
        }
    }

    private void SaveConfigImmediately(Config config)
    {
        var copy = new Config(config);
        lock (_saveLock)
        {
            _appDataFolder.EnsurePathExists();
            var path = GetUserConfigPath();
            using var stream = _env.FileSystem.File.Open(path, FileMode.Create);
            JsonSerializer.Serialize(stream, copy);
        }
    }
}
