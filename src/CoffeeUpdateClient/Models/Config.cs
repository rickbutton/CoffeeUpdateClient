using System.Text.Json.Serialization;

namespace CoffeeUpdateClient.Models;

public class Config
{
    [JsonInclude]
    [JsonPropertyName("AddOnsPath")]
    private string _addOnsPath = string.Empty;

    [JsonIgnore]
    public string AddOnsPath
    {
        get => _addOnsPath;
        set
        {
            _addOnsPath = value;
            ConfigUpdated?.Invoke(this);
        }
    }

    public delegate void ConfigUpdatedHandler(Config config);
    public event ConfigUpdatedHandler? ConfigUpdated;

    private static Config? _instance;
    public static void InitConfigSingleton(Config config)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException("Config.Instance is already initialized.");
        }
        _instance = config;
    }

    public static void ClearConfigSingleton()
    {
        if (_instance == null)
        {
            throw new InvalidOperationException("Config.Instance is already cleared.");
        }
        _instance = null;
    }

    public static Config Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("Config.Instance has not been initialized.");
            }
            return _instance!;
        }
    }
}