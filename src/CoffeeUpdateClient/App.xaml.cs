using Path = System.IO.Path;
using System.Windows;
using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
        var services = new ServiceCollection();

        // services
        services.AddSingleton<IEnv, WindowsEnv>();
        services.AddSingleton<IConfigService, FileSystemConfigService>();
        services.AddSingleton<IWoWLocator, RegistryWoWLocator>();
        services.AddSingleton<IAddOnDownloader, HttpsAddOnDownloader>();

        // utilities
        services.AddSingleton<AppDataFolder>();
        services.AddSingleton<LocalAddOnMetadataLoader>();
        services.AddSingleton<AddOnBundleInstaller>();

        // main window
        services.AddSingleton<MainWindow>();

        var provider = services.BuildServiceProvider();

        // ensure the app data folder exists
        var appDataFolder = provider.GetRequiredService<AppDataFolder>();
        appDataFolder.EnsurePathExists();

        // setup serilog
        var logPath = Path.Combine(
            appDataFolder.GetPath(),
            "client.log"
        );
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("starting CoffeeUpdateClient");
        var config = await provider.GetRequiredService<IConfigService>().LoadConfigSingleton();
        Log.Information("loaded config", config);

        var window = provider.GetRequiredService<MainWindow>();
        window.Show();
    }
}

