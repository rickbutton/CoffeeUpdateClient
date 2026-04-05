namespace CoffeeUpdater.Services;

public interface IRegistryReader
{
    IRegistryKeyReader? OpenSubKey(string path);
    IRegistryKeyReader? OpenSubKey(string path, bool writable);
}

public interface IRegistryKeyReader : IDisposable
{
    string[] GetSubKeyNames();
    IRegistryKeyReader? OpenSubKey(string name);
    object? GetValue(string name);
    void SetValue(string name, object value);
    void DeleteValue(string name, bool throwOnMissingValue);
}
