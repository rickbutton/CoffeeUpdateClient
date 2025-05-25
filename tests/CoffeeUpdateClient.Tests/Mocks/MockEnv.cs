using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using CoffeeUpdateClient.Services;

namespace CoffeeUpdateClient.Tests.Mocks;

public class MockEnv : IEnv
{
    public string GetUserAppDataFolderPath()
    {
        return @"C:\Users\testuser\AppData\Roaming";
    }

    public MockFileSystem MockFileSystem { get; } = new MockFileSystem();
    public IFileSystem FileSystem { get => MockFileSystem; }

    public MockEnv()
    {
        MockFileSystem.AddDirectory(GetUserAppDataFolderPath());
    }
}