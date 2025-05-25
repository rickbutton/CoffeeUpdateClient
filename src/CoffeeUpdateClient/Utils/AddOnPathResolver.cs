using Path = System.IO.Path;
using Serilog;

namespace CoffeeUpdateClient.Utils;

public class AddOnPathResolver
{
    public static string? NormalizeAddOnsDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Log.Debug($"NormalizeAddOnsDirectory: path=null");
            return null;
        }

        path = Path.GetFullPath(path);
        Log.Debug($"NormalizeAddOnsDirectory: input={path}");

        var dirName = Path.GetFileName(path);
        var parentDirName = Path.GetFileName(Path.GetDirectoryName(path));
        var parentParentDirName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
        Log.Debug($"NormalizeAddOnsDirectory: dirName={dirName} parentDirName={parentDirName} parentParentDirName={parentParentDirName}");
        if (dirName == "AddOns" && parentDirName == "Interface" && parentParentDirName == "_retail_")
        {
            Log.Information($"NormalizeAddOnsDirectory: input={path} output={path}");
            return path;
        }
        else if (dirName == "Interface" && parentDirName == "_retail_")
        {
            var output = Path.Join(path, "AddOns");
            Log.Information($"NormalizeAddOnsDirectory: path={path} output={output}");
            return output;
        }
        else if (dirName == "_retail_")
        {
            var output = Path.Join(path, "Interface", "AddOns");
            Log.Information($"NormalizeAddOnsDirectory: path={path} output={output}");
            return output;
        }
        else if (dirName == "World of Warcraft")
        {
            var output = Path.Join(path, "_retail_", "Interface", "AddOns");
            Log.Information($"NormalizeAddOnsDirectory: path={path} output={output}");
            return output;
        }
        else
        {
            Log.Information($"NormalizeAddOnsDirectory: path={path} output=null");
            return null;
        }
    }
}