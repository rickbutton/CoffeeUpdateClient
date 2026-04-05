using CoffeeUpdater.Models;

namespace CoffeeUpdater.Services;

public interface IConfigService
{
    public Task<Config> GetConfigAsync();
}