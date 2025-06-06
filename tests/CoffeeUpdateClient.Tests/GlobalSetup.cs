using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Tests.Mocks;
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