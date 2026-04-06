using Path = System.IO.Path;
using CoffeeUpdater.Services;

namespace CoffeeUpdater.Utils;

public class AppDataFolder
{
    private readonly IEnv _env;

    public AppDataFolder(IEnv env)
    {
        _env = env;
    }

    public string GetPath()
    {
        return Path.Combine(
            _env.GetUserAppDataFolderPath(),
            "CoffeeUpdateClient"
        );
    }

    public void EnsurePathExists()
    {
        var path = GetPath();
        if (!_env.FileSystem.Directory.Exists(path))
        {
            _env.FileSystem.Directory.CreateDirectory(path);
        }
    }
}