using CoffeeUpdateClient.Models;

namespace CoffeeUpdateClient.Services;

public interface IConfigService
{
    public Task<Config> GetConfigAsync();
}