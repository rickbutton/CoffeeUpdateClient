using CoffeeUpdater.Services;

namespace CoffeeUpdater.Tests.Mocks;

public class MockRegistryReader : IRegistryReader
{
    private readonly Dictionary<string, MockRegistryKeyReader?> _keys = new();

    public MockRegistryKeyReader AddKey(string path)
    {
        var key = new MockRegistryKeyReader();
        _keys[path] = key;
        return key;
    }

    public IRegistryKeyReader? OpenSubKey(string path) =>
        _keys.TryGetValue(path, out var key) ? key : null;
}

public class MockRegistryKeyReader : IRegistryKeyReader
{
    private readonly Dictionary<string, MockRegistryKeyReader?> _subKeys = new();
    private readonly Dictionary<string, object?> _values = new();

    public MockRegistryKeyReader AddSubKey(string name)
    {
        var key = new MockRegistryKeyReader();
        _subKeys[name] = key;
        return key;
    }

    public void AddNullSubKey(string name) => _subKeys[name] = null;

    public void SetValue(string name, object? value) => _values[name] = value;

    public string[] GetSubKeyNames() => [.. _subKeys.Keys];

    public IRegistryKeyReader? OpenSubKey(string name) =>
        _subKeys.TryGetValue(name, out var key) ? key : null;

    public object? GetValue(string name) =>
        _values.TryGetValue(name, out var value) ? value : null;

    public void Dispose() { }
}
