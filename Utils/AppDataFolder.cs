using System.IO;

namespace CoffeeUpdateClient.Utils;

public class AppDataFolder
{
    public static string GetPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CoffeeUpdateClient"
        );
    }

    public static void EnsurePathExists()
    {
        var path = GetPath();
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}