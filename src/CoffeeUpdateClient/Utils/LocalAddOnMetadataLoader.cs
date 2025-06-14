using Path = System.IO.Path;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoffeeUpdateClient.Utils;

public class LocalAddOnMetadataLoader
{
    private readonly IEnv _env;

    public enum Status
    {
        Found,
        NotFound,
        Error,
    }

    // List of possible TOC file name formats
    private readonly string[] _tocFileFormats = new string[]
    {
        "{0}.toc",
        "{0}_Mainline.toc",
        "{0}_Standard.toc"
    };

    public LocalAddOnMetadataLoader(IEnv env)
    {
        _env = env;
    }

    public async Task<(AddOnMetadata?, Status)> LoadAddOnMetadataAsync(string name)
    {
        var addOnsPath = Config.Instance.AddOnsPath;

        if (string.IsNullOrEmpty(addOnsPath))
        {
            Log.Debug("AddOns path is null or empty.");
            return (null, Status.NotFound);
        }

        var addOnPath = Path.Combine(addOnsPath, name);

        if (!_env.FileSystem.Directory.Exists(addOnPath))
        {
            Log.Debug("AddOn directory does not exist: {Path}", addOnPath);
            return (null, Status.NotFound);
        }

        // Try each possible TOC filename format
        foreach (var format in _tocFileFormats)
        {
            var tocFileName = string.Format(format, name);
            var tocPath = Path.Combine(addOnPath, tocFileName);

            if (!_env.FileSystem.File.Exists(tocPath))
            {
                Log.Debug("TOC file not found: {Path}", tocPath);
                continue;
            }

            Log.Debug("Found TOC file: {Path}", tocPath);
            var tocContent = await _env.FileSystem.File.ReadAllTextAsync(tocPath);
            var version = TOCParser.GetVersion(tocContent);

            if (string.IsNullOrEmpty(version))
            {
                Log.Warning("Version not found in TOC file for AddOn: {AddOnName} at {Path}", name, tocPath);
                continue;
            }

            return (new AddOnMetadata
            {
                Name = name,
                Version = version,
            }, Status.Found);
        }

        Log.Error("No valid TOC file found for AddOn: {AddOnName}", name);
        return (null, Status.Error);
    }
}