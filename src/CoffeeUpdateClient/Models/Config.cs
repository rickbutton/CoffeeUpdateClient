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
}
