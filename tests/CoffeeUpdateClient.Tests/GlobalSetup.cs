
using Serilog;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void InitLogging()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
    }
}