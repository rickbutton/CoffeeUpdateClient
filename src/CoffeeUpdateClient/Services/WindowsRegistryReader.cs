using Microsoft.Win32;

namespace CoffeeUpdateClient.Services;

public class WindowsRegistryReader : IRegistryReader
{
    public IRegistryKeyReader? OpenSubKey(string path)
    {
        var key = Registry.LocalMachine.OpenSubKey(path);
        return key == null ? null : new WindowsRegistryKeyReader(key);
    }
}

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

    public void Dispose() => _key.Dispose();
}
