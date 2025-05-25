using Path = System.IO.Path;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using Serilog;

namespace CoffeeUpdateClient.Utils;

public class LocalAddOnMetadataLoader
{
    private readonly IEnv _env;
    private readonly IConfigService _configService;
    public LocalAddOnMetadataLoader(IEnv env, IConfigService configService)
    {
        _env = env;
        _configService = configService;
    }

    public async Task<AddOnMetadata?> LoadAddOnMetadata(string name)
    {
        var addOnsPath = _configService.Instance.AddOnsPath;

        var addOnPath = Path.Combine(addOnsPath, name);
        var tocPath = Path.Combine(addOnPath, $"{name}.toc");

        if (!_env.FileSystem.File.Exists(tocPath))
        {
            Log.Warning("TOC file not found for AddOn: {AddOnName} at {Path}", name, tocPath);
            return null;
        }

        var tocContent = await _env.FileSystem.File.ReadAllTextAsync(tocPath);
        var version = TOCParser.GetVersion(tocContent);

        if (string.IsNullOrEmpty(version))
        {
            Log.Warning("Version not found in TOC file for AddOn: {AddOnName} at {Path}", name, tocPath);
            return null;
        }

        return new AddOnMetadata
        {
            Name = name,
            Version = version,
        };
    }
}