using CoffeeUpdater.Services;

namespace CoffeeUpdater.Utils;

public class VelopackHooks
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CoffeeUpdater";

    private readonly IRegistryReader _registry;
    private readonly string? _exePath;

    public VelopackHooks(IRegistryReader registry) : this(registry, Environment.ProcessPath) { }

    public VelopackHooks(IRegistryReader registry, string? exePath)
    {
        _registry = registry;
        _exePath = exePath;
    }

    public void OnInstall()
    {
        if (_exePath == null) return;

        using var key = _registry.OpenSubKey(RunKeyPath, writable: true);
        key?.SetValue(AppName, $"\"{_exePath}\"");
    }

    public void OnUninstall()
    {
        using var key = _registry.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
