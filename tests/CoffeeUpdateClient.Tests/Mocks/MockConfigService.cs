using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;

namespace CoffeeUpdateClient.Tests.Mocks;

public class MockConfigService : IConfigService
{
    private Config _config;
    private bool _isInitialized;

    public MockConfigService(string addOnsPath = @"C:\World of Warcraft\_retail_\Interface\AddOns")
    {
        _config = new Config
        {
            AddOnsPath = addOnsPath
        };
    }

    public Config Instance
    {
        get
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Call LoadConfigSingleton() first");
            }
            return _config;
        }
    }

    public Task<Config> LoadConfigSingleton()
    {
        _isInitialized = true;
        return Task.FromResult(_config);
    }
}
