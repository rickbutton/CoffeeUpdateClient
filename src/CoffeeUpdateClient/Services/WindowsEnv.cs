using System.IO.Abstractions;

namespace CoffeeUpdateClient.Services;

public class WindowsEnv : IEnv
{
    public string GetUserAppDataFolderPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    public IFileSystem FileSystem { get; } = new FileSystem();
}