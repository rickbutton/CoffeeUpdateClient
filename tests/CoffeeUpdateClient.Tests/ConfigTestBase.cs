
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Tests.Mocks;

namespace CoffeeUpdateClient.Tests;

public class ConfigTestBase
{
    [SetUp]
    public async Task LoadConfigAsync()
    {
        var mockConfigService = new MockConfigService();
        Config.InitConfigSingleton(await mockConfigService.GetConfigAsync());
    }

    [TearDown]
    public void ClearConfig()
    {
        Config.ClearConfigSingleton();
    }
}