using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using Serilog;
using Path = System.IO.Path;

namespace CoffeeUpdateClient;

public partial class App : Application
{
    private const string SetupUrl =
        "https://coffee-auras.nyc3.digitaloceanspaces.com/releases/CoffeeUpdater-win-Setup.exe";

    private static readonly string V2ExePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CoffeeUpdater", "current", "CoffeeUpdater.exe");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupUnhandledExceptionHandling();
        _ = OnStartupAsync();
    }

    private async Task OnStartupAsync()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CoffeeUpdateClient");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logDir, "bridge.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            if (File.Exists(V2ExePath))
            {
                Log.Information("v2 found at {Path}, launching", V2ExePath);
                Process.Start(new ProcessStartInfo(V2ExePath) { UseShellExecute = true });
                Shutdown(0);
                return;
            }

            Log.Information("v2 not found, downloading setup from {Url}", SetupUrl);
            var tempPath = Path.Combine(Path.GetTempPath(), "CoffeeUpdater-win-Setup.exe");

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(SetupUrl);
            response.EnsureSuccessStatusCode();

            await using var fs = new FileStream(tempPath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            fs.Close();

            Log.Information("Setup downloaded to {Path}, launching", tempPath);
            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            Shutdown(0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bridge update failed");
            MessageBox.Show(
                "Could not automatically install CoffeeUpdater v2.\n\n" +
                "Please download the latest version manually from:\n" +
                SetupUrl,
                "Coffee Updater",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
        }
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

    private static void ShowUnhandledException(Exception? e, string unhandledExceptionType)
    {
        Log.Error("Unhandled exception ({type}): {e}", unhandledExceptionType, e);
    }
}
