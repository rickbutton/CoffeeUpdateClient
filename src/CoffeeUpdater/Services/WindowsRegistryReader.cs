using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

namespace CoffeeUpdater.Services;

[ExcludeFromCodeCoverage]
public class WindowsRegistryReader : IRegistryReader
{
    public IRegistryKeyReader? OpenSubKey(string path)
    {
        var key = Registry.LocalMachine.OpenSubKey(path);
        return key == null ? null : new WindowsRegistryKeyReader(key);
    }

    public IRegistryKeyReader? OpenSubKey(string path, bool writable)
    {
        var key = Registry.CurrentUser.OpenSubKey(path, writable);
        return key == null ? null : new WindowsRegistryKeyReader(key);
    }
}

[ExcludeFromCodeCoverage]
internal sealed class WindowsRegistryKeyReader : IRegistryKeyReader
{
    private readonly RegistryKey _key;

    public WindowsRegistryKeyReader(RegistryKey key) => _key = key;

    public string[] GetSubKeyNames() => _key.GetSubKeyNames();

    public IRegistryKeyReader? OpenSubKey(string name)
    {
        var key = _key.OpenSubKey(name);
        return key == null ? null : new WindowsRegistryKeyReader(key);
    }

    public object? GetValue(string name) => _key.GetValue(name);

    public void SetValue(string name, object value) => _key.SetValue(name, value);

    public void DeleteValue(string name, bool throwOnMissingValue) =>
        _key.DeleteValue(name, throwOnMissingValue);

    public void Dispose() => _key.Dispose();
}
