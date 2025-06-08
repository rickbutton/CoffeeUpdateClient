using Path = System.IO.Path;
using System.Windows;
using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using CoffeeUpdateClient.Models;
using AutoUpdaterDotNET;
using System.Diagnostics;

namespace CoffeeUpdateClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupUnhandledExceptionHandling();
        _ = OnStartupAsync(e);
    }

    protected async Task OnStartupAsync(StartupEventArgs e)
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
        services.AddSingleton<AddOnUpdateManager>();
        services.AddSingleton<InstallLogCollector>();

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



        var config = await provider.GetRequiredService<IConfigService>().GetConfigAsync();
        Config.InitConfigSingleton(config);
        Log.Verbose("loaded config", config);

        var window = provider.GetRequiredService<MainWindow>();
        window.Show();

        AutoUpdater.Synchronous = true;
        AutoUpdater.ShowSkipButton = false;
        AutoUpdater.ShowRemindLaterButton = false;
        AutoUpdater.TopMost = true;
#if DEBUG
        Log.Information("skipping updater in DEBUG");
#else
        var clientManifest = "https://coffee-auras.nyc3.digitaloceanspaces.com/client.xml";
        Log.Information("checking for updates via manifest at {manifest}", clientManifest);
        AutoUpdater.SetOwner(window);
        AutoUpdater.Start(clientManifest);
#endif
    }

    private void SetupUnhandledExceptionHandling()
    {
        // Catch exceptions from all threads in the AppDomain.
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            ShowUnhandledException(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");

        // Catch exceptions from each AppDomain that uses a task scheduler for async operations.
        TaskScheduler.UnobservedTaskException += (sender, args) =>
            ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException");

        // Catch exceptions from a single specific UI dispatcher thread.
        Dispatcher.UnhandledException += (sender, args) =>
        {
            // If we are debugging, let Visual Studio handle the exception and take us to the code that threw it.
            if (!Debugger.IsAttached)
            {
                args.Handled = true;
                ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException");
            }
        };
    }

    void ShowUnhandledException(Exception? e, string unhandledExceptionType)
    {
        Log.Error("Unhandled exception ({type}): {e}", unhandledExceptionType, e);
    }
}

