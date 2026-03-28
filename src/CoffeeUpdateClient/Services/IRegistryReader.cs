namespace CoffeeUpdateClient.Services;

public interface IRegistryReader
{
    IRegistryKeyReader? OpenSubKey(string path);
}

public interface IRegistryKeyReader : IDisposable
{
    string[] GetSubKeyNames();
    IRegistryKeyReader? OpenSubKey(string name);
    object? GetValue(string name);
}
