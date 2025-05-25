using System.IO.Abstractions.TestingHelpers;
using CoffeeUpdateClient.Tests.Mocks;
using CoffeeUpdateClient.Utils;

namespace CoffeeUpdateClient.Tests;

[TestFixture]
public class AppDataFolderTest
{
    [Test]
    public void EnsurePathExists_CreatesDirectoryWhenNotExists()
    {
        var env = new MockEnv();
        var appDataFolder = new AppDataFolder(env);

        appDataFolder.EnsurePathExists();

        Assert.That(env.FileSystem.Directory.Exists(@"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient"), Is.True);
    }

    [Test]
    public void EnsurePathExists_DoesNotThrowWhenDirectoryAlreadyExists()
    {
        var env = new MockEnv();
        env.MockFileSystem.AddDirectory(@"C:\Users\testuser\AppData\Roaming\CoffeeUpdateClient");
        var appDataFolder = new AppDataFolder(env);

        Assert.DoesNotThrow(() => appDataFolder.EnsurePathExists());
    }
}