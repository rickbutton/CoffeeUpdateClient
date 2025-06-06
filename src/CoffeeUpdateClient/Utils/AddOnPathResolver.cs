using Path = System.IO.Path;
using Serilog;

namespace CoffeeUpdateClient.Utils;

public class AddOnPathResolver
{
    private const string RetailFolder = "_retail_";
    private const string InterfaceFolder = "Interface";
    private const string AddOnsFolder = "AddOns";
    private const string WowFolder = "World of Warcraft";

    private class PathPattern
    {
        public Func<List<string>, bool> Condition { get; }
        public string[] SegmentsToAppend { get; }

        public PathPattern(Func<List<string>, bool> condition, string[] segmentsToAppend)
        {
            Condition = condition;
            SegmentsToAppend = segmentsToAppend;
        }
    }

    public static string? NormalizeAddOnsDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Log.Debug("NormalizeAddOnsDirectory: path is null or empty");
            return null;
        }

        Log.Debug($"NormalizeAddOnsDirectory: input={path}");

        var segments = GetPathSegments(path);
        Log.Debug($"NormalizeAddOnsDirectory: segments=[{string.Join(", ", segments)}]");

        if (IsValidAddOnsPath(segments))
        {
            Log.Debug($"NormalizeAddOnsDirectory: Path is already valid. input='{path}', output='{path}'");
            return path;
        }

        string? output = null;

        var patterns = new List<PathPattern>
        {
            new(
                segs => segs.Count >= 2 && segs[^1] == InterfaceFolder && segs[^2] == RetailFolder,
                [AddOnsFolder]),
            new(
                segs => segs.Count >= 1 && segs[^1] == RetailFolder,
                [InterfaceFolder, AddOnsFolder]),
            new(
                segs => segs.Count >= 1 && segs[^1] == WowFolder,
                [RetailFolder, InterfaceFolder, AddOnsFolder])
        };

        foreach (var pattern in patterns)
        {
            if (pattern.Condition(segments))
            {
                output = path; // Start with the original full path
                foreach (var segmentToAppend in pattern.SegmentsToAppend)
                {
                    output = Path.Join(output, segmentToAppend);
                }
                Log.Debug($"NormalizeAddOnsDirectory: Pattern matched. input='{path}', output='{output}'");
                break;
            }
        }

        if (output == null)
        {
            Log.Debug($"NormalizeAddOnsDirectory: No pattern matched. input='{path}', output=null");
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

        return segments[^1] == AddOnsFolder &&
               segments[^2] == InterfaceFolder &&
               segments[^3] == RetailFolder;
    }
}