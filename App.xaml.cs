using System.IO;
using System.Reflection;
using System.Windows;
using CoffeeUpdateClient.Utils;
using ReactiveUI;
using Serilog;
using Splat;

namespace CoffeeUpdateClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        OnStartupAsync(e);
    }

    protected async void OnStartupAsync(StartupEventArgs e)
    {
        Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());

        AppDataFolder.EnsurePathExists();
        var logPath = Path.Combine(
            AppDataFolder.GetPath(),
            "client.log"
        );
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Starting CoffeeUpdateClient");
        await ConfigLoader.LoadConfigSingleton();
        Log.Information("loaded config", ConfigLoader.Instance);

        var window = new MainWindow(ConfigLoader.Instance.AddOnsPath);
        window.Show();
    }
}

