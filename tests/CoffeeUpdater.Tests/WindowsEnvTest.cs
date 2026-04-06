using CoffeeUpdater.Services;
using NUnit.Framework;

namespace CoffeeUpdater.Tests;

public class WindowsEnvTest
{
    [Test]
    public void GetUserAppDataFolderPath_ReturnsNonEmptyString()
    {
        var env = new WindowsEnv();
        Assert.That(env.GetUserAppDataFolderPath(), Is.Not.Empty);
    }

    [Test]
    public void FileSystem_IsNotNull()
    {
        var env = new WindowsEnv();
        Assert.That(env.FileSystem, Is.Not.Null);
    }
}
