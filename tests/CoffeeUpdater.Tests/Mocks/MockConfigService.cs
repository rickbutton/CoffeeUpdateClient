using CoffeeUpdater.Models;
using CoffeeUpdater.Services;

namespace CoffeeUpdater.Tests.Mocks;

public class MockConfigService : IConfigService
{
    private Config _config;

    public MockConfigService(string addOnsPath = @"C:\World of Warcraft\_retail_\Interface\AddOns")
    {
        _config = new Config
        {
            AddOnsPath = addOnsPath
        };
    }

    public Task<Config> GetConfigAsync()
    {
        return Task.FromResult(_config);
    }
}
