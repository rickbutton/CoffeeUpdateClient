using System.Text.Json.Serialization;

namespace CoffeeUpdater.Models;

public class Config
{
    // Lock protects _installedAddOnFolders from concurrent mutation/read corruption.
    // _addOnsPath is a reference type (atomic read/write) and doesn't need locking for individual access.
    // The copy constructor acquires the lock to get a consistent view of ALL fields for serialization.
    private readonly object _lock = new();

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
    public IReadOnlyDictionary<string, List<string>> InstalledAddOnFolders
    {
        get { lock (_lock) { return new Dictionary<string, List<string>>(_installedAddOnFolders); } }
    }

    public void SetInstalledFolders(string addonName, List<string> folders)
    {
        lock (_lock) { _installedAddOnFolders[addonName] = folders; }
        ConfigUpdated?.Invoke(this);
    }

    public void RemoveInstalledFolders(string addonName)
    {
        lock (_lock) { _installedAddOnFolders.Remove(addonName); }
        ConfigUpdated?.Invoke(this);
    }

    /// <summary>
    /// Copy constructor — acquires the source's lock to produce a consistent deep copy.
    /// </summary>
    public Config(Config other)
    {
        lock (other._lock)
        {
            _addOnsPath = other._addOnsPath;
            _installedAddOnFolders = other._installedAddOnFolders.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<string>(kvp.Value));
        }
    }

    public Config() { }

    public delegate void ConfigUpdatedHandler(Config config);
    public event ConfigUpdatedHandler? ConfigUpdated;
}
