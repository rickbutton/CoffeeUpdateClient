using CoffeeUpdater.Services;

namespace CoffeeUpdater.Tests.Mocks;

class MockWowLocator : IWoWLocator
{
    private readonly string? _installPath;

    public MockWowLocator(string? installPath = null)
    {
        _installPath = installPath;
    }

    public string? GetWoWInstallPath()
    {
        return _installPath;
    }
}