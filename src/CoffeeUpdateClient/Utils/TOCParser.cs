using System.Linq;

namespace CoffeeUpdateClient.Utils;

public static class TOCParser
{
    public static string? GetVersion(string? tocContent)
    {
        if (string.IsNullOrEmpty(tocContent))
        {
            return null;
        }

        var lines = tocContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        foreach (var line in lines)
        {
            if (line.StartsWith("## Version:"))
            {
                return line.Substring("## Version:".Length).Trim();
            }
        }
        return null;
    }
}