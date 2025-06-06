using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;

namespace CoffeeUpdateClient.Tests.Mocks;

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
