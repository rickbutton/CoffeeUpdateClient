using CoffeeUpdater.Tests.Mocks;
using CoffeeUpdater.Utils;

namespace CoffeeUpdater.Tests;

[TestFixture]
public class VelopackHooksTest
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    [Test]
    public void OnInstall_SetsRunKeyWithQuotedExePath()
    {
        var registry = new MockRegistryReader();
        var runKey = registry.AddKey(RunKeyPath);

        var hooks = new VelopackHooks(registry, @"C:\Apps\CoffeeUpdater\CoffeeUpdater.exe");
        hooks.OnInstall();

        Assert.That(runKey.GetValue("CoffeeUpdater"), Is.EqualTo(@"""C:\Apps\CoffeeUpdater\CoffeeUpdater.exe"""));
    }

    [Test]
    public void OnInstall_NullExePath_DoesNothing()
    {
        var registry = new MockRegistryReader();
        var runKey = registry.AddKey(RunKeyPath);

        var hooks = new VelopackHooks(registry, null);
        hooks.OnInstall();

        Assert.That(runKey.GetValue("CoffeeUpdater"), Is.Null);
    }

    [Test]
    public void OnInstall_RunKeyNotFound_DoesNotThrow()
    {
        var registry = new MockRegistryReader(); // no keys added

        var hooks = new VelopackHooks(registry, @"C:\Apps\CoffeeUpdater.exe");

        Assert.DoesNotThrow(() => hooks.OnInstall());
    }

    [Test]
    public void OnUninstall_RemovesRunKey()
    {
        var registry = new MockRegistryReader();
        var runKey = registry.AddKey(RunKeyPath);
        runKey.SetValue("CoffeeUpdater", @"""C:\Apps\CoffeeUpdater.exe""");

        var hooks = new VelopackHooks(registry, @"C:\Apps\CoffeeUpdater.exe");
        hooks.OnUninstall();

        Assert.That(runKey.GetValue("CoffeeUpdater"), Is.Null);
    }

    [Test]
    public void OnUninstall_KeyNotPresent_DoesNotThrow()
    {
        var registry = new MockRegistryReader();
        var runKey = registry.AddKey(RunKeyPath);

        var hooks = new VelopackHooks(registry, @"C:\Apps\CoffeeUpdater.exe");

        Assert.DoesNotThrow(() => hooks.OnUninstall());
    }

    [Test]
    public void OnUninstall_RunKeyNotFound_DoesNotThrow()
    {
        var registry = new MockRegistryReader(); // no keys added

        var hooks = new VelopackHooks(registry, @"C:\Apps\CoffeeUpdater.exe");

        Assert.DoesNotThrow(() => hooks.OnUninstall());
    }
}
