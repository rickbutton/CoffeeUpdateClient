using Path = System.IO.Path;
using Serilog;
using System.Collections.Generic;

namespace CoffeeUpdateClient.Utils;

public class AddOnPathResolver
{
    private static readonly string[] ExpectedPathSegments = { "_retail_", "Interface", "AddOns" };
    public static string? NormalizeAddOnsDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Log.Debug($"NormalizeAddOnsDirectory: path=null");
            return null;
        }

        path = Path.GetFullPath(path);
        Log.Debug($"NormalizeAddOnsDirectory: input={path}");

        var segments = GetPathSegments(path);
        Log.Debug($"NormalizeAddOnsDirectory: segments={string.Join(", ", segments)}");

        if (IsValidAddOnsPath(segments))
        {
            Log.Information($"NormalizeAddOnsDirectory: input={path} output={path}");
            return path;
        }

        string? output = null;

        var patterns = new Dictionary<Func<List<string>, bool>, string[]>
        {
            { segs => segs.Count >= 2 && segs[^1] == "Interface" && segs[^2] == "_retail_",
              new[] { "AddOns" } },

            { segs => segs.Count >= 1 && segs[^1] == "_retail_",
              new[] { "Interface", "AddOns" } },

            { segs => segs.Count >= 1 && segs[^1] == "World of Warcraft",
              new[] { "_retail_", "Interface", "AddOns" } }
        };

        foreach (var pattern in patterns)
        {
            if (pattern.Key(segments))
            {
                output = path;
                foreach (var segment in pattern.Value)
                {
                    output = Path.Join(output, segment);
                }
                Log.Information($"NormalizeAddOnsDirectory: path={path} output={output}");
                break;
            }
        }

        if (output == null)
        {
            Log.Information($"NormalizeAddOnsDirectory: path={path} output=null");
        }

        return output;
    }
    private static List<string> GetPathSegments(string path)
    {
        var segments = new List<string>();
        string? current = path;

        while (!string.IsNullOrEmpty(current))
        {
            string segment = Path.GetFileName(current);
            if (!string.IsNullOrEmpty(segment))
            {
                segments.Insert(0, segment);
            }
            current = Path.GetDirectoryName(current);
        }

        return segments;
    }

    private static bool IsValidAddOnsPath(List<string> segments)
    {
        if (segments.Count < 3)
            return false;

        return segments[^1] == "AddOns" &&
               segments[^2] == "Interface" &&
               segments[^3] == "_retail_";
    }
}