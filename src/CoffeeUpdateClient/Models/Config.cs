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

    [JsonInclude]
    [JsonPropertyName("InstalledAddOnFolders")]
    private Dictionary<string, List<string>> _installedAddOnFolders = new();

    [JsonIgnore]
    public IReadOnlyDictionary<string, List<string>> InstalledAddOnFolders => _installedAddOnFolders;

    public void SetInstalledFolders(string addonName, List<string> folders)
    {
        _installedAddOnFolders[addonName] = folders;
        ConfigUpdated?.Invoke(this);
    }

    public void RemoveInstalledFolders(string addonName)
    {
        _installedAddOnFolders.Remove(addonName);
        ConfigUpdated?.Invoke(this);
    }

    public delegate void ConfigUpdatedHandler(Config config);
    public event ConfigUpdatedHandler? ConfigUpdated;
}
