using Path = System.IO.Path;
using System.Net.Http;
using System.Windows;
using CoffeeUpdater.Services;
using CoffeeUpdater.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;
using System.Diagnostics;

namespace CoffeeUpdater;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private SingleInstanceGuard? _singleInstanceGuard;
    private IHost? _host;
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance check
        _singleInstanceGuard = new SingleInstanceGuard();
        if (!_singleInstanceGuard.TryAcquire())
        {
            SingleInstanceGuard.SignalExistingInstance();
            Shutdown();
            return;
        }

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

            Log.Information("starting CoffeeUpdater");

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
            services.AddSingleton<AddOnSyncService>();
            services.AddSingleton<AppUpdateService>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            // Start the tray icon
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayIcon.DoubleClickCommand = new RelayCommand(ShowSettingsWindow);

            // Update status text whenever the context menu opens
            _trayIcon.ContextMenu.Opened += (_, _) => UpdateStatusMenuItem();

            // Listen for IPC "show" commands from second instances
            _singleInstanceGuard!.ShowRequested += () =>
                Dispatcher.Invoke(ShowSettingsWindow);
            _singleInstanceGuard.StartListening();

            // Build and start the host for background services
            var hostBuilder = Host.CreateDefaultBuilder();
            hostBuilder.ConfigureServices(hostServices =>
            {
                hostServices.AddHostedService(sp => _serviceProvider.GetRequiredService<AddOnSyncService>());
#if !DEBUG
                hostServices.AddHostedService(sp => _serviceProvider.GetRequiredService<AppUpdateService>());
#endif
            });
            _host = hostBuilder.Build();
            await _host.StartAsync();

            // If AddOns path is not configured, show the window so the user can set it
            if (string.IsNullOrEmpty(config.AddOnsPath))
            {
                Log.Information("AddOns path not configured, showing settings window for initial setup");
                ShowSettingsWindow();
            }
        }
        catch (Exception ex)
        {
            var message = $"A fatal error occurred during startup:\n\n{ex.Message}";
            Log.Fatal(ex, "Fatal startup error");
            MessageBox.Show(message, "Coffee Updater", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ShowSettingsWindow()
    {
        var window = _serviceProvider?.GetRequiredService<MainWindow>();
        if (window == null) return;

        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    private void UpdateStatusMenuItem()
    {
        var syncService = _serviceProvider?.GetService<AddOnSyncService>();
        if (syncService?.LastSyncTime == null || _trayIcon?.ContextMenu == null) return;

        var statusItem = _trayIcon.ContextMenu.Items
            .OfType<MenuItem>()
            .FirstOrDefault(m => m.Name == "StatusMenuItem");
        if (statusItem == null) return;

        var elapsed = DateTime.Now - syncService.LastSyncTime.Value;
        statusItem.Header = $"Coffee Updater - Last Updated: {FormatTimeAgo(elapsed)}";
    }

    private static string FormatTimeAgo(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 60) return "just now";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
        return $"{(int)elapsed.TotalDays}d ago";
    }

    private void TraySettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsWindow();
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        _trayIcon?.Dispose();
        _singleInstanceGuard?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void SetupUnhandledExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            ShowUnhandledException(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");

        TaskScheduler.UnobservedTaskException += (sender, args) =>
            ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException");

        Dispatcher.UnhandledException += (sender, args) =>
        {
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
