using CoffeeUpdater.Models;
using CoffeeUpdater.Tests.Mocks;
using Serilog;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void InitLoggingAsync()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
    }
}