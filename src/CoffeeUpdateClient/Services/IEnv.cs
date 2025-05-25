using System.IO.Abstractions;

namespace CoffeeUpdateClient.Services;

public interface IEnv
{
    string GetUserAppDataFolderPath();
    IFileSystem FileSystem { get; }
}