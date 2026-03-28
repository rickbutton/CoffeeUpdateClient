using Path = System.IO.Path;
using System.Net.Http;
using System.Windows;
using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
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
        try
        {
            // Bootstrap: create the objects needed to load config before DI is available.
            var env = new WindowsEnv();
            var appDataFolder = new AppDataFolder(env);
            appDataFolder.EnsurePathExists();

            var logPath = Path.Combine(appDataFolder.GetPath(), "client.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("starting CoffeeUpdateClient");

            var wowLocator = new RegistryWoWLocator(new WindowsRegistryReader());
            var configService = new FileSystemConfigService(env, appDataFolder, wowLocator);
            var config = await configService.GetConfigAsync();
            Log.Debug("loaded config: {@Config}", config);

            // Build DI container with all bootstrapped instances registered directly.
            var services = new ServiceCollection();
            services.AddSingleton<IEnv>(env);
            services.AddSingleton<IWoWLocator>(wowLocator);
            services.AddSingleton(appDataFolder);
            services.AddSingleton<IConfigService>(configService);
            services.AddSingleton(config);
            services.AddSingleton(new HttpClient());

            var localManifestDir = GetLocalManifestDir(e.Args);
            if (localManifestDir != null)
            {
                Log.Information("Using local manifest from {Dir}", localManifestDir);
                services.AddSingleton<IAddOnDownloader>(new LocalFileAddOnDownloader(env.FileSystem, localManifestDir));
            }
            else
            {
                services.AddSingleton<IAddOnDownloader, HttpsAddOnDownloader>();
            }
            services.AddSingleton<LocalAddOnMetadataLoader>();
            services.AddSingleton<AddOnBundleInstaller>();
            services.AddSingleton<AddOnUpdateManager>();
            services.AddSingleton<InstallLogCollector>();
            services.AddSingleton<MainWindow>();

            var provider = services.BuildServiceProvider();

            var window = provider.GetRequiredService<MainWindow>();
            window.Show();

            AutoUpdater.Synchronous = true;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.TopMost = true;
            AutoUpdater.RunUpdateAsAdmin = false;
#if DEBUG
            Log.Information("skipping updater in DEBUG");
#else
            var clientManifest = "https://coffee-auras.nyc3.digitaloceanspaces.com/client.xml";
            Log.Information("checking for updates via manifest at {manifest}", clientManifest);
            AutoUpdater.SetOwner(window);
            AutoUpdater.Start(clientManifest);
#endif
        }
        catch (Exception ex)
        {
            var message = $"A fatal error occurred during startup:\n\n{ex.Message}";
            Log.Fatal(ex, "Fatal startup error");
            MessageBox.Show(message, "Coffee AddOn Updater", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
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

    static string? GetLocalManifestDir(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--local-manifest")
                return args[i + 1];
        }
        return null;
    }

    static void ShowUnhandledException(Exception? e, string unhandledExceptionType)
    {
        Log.Error("Unhandled exception ({type}): {e}", unhandledExceptionType, e);
    }
}
