using System.IO.Abstractions;

namespace CoffeeUpdater.Services;

public interface IEnv
{
    string GetUserAppDataFolderPath();
    IFileSystem FileSystem { get; }
}