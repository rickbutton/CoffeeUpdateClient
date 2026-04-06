using CoffeeUpdater.Services;
using CoffeeUpdater.Utils;
using Velopack;

namespace CoffeeUpdater;

public enum StartupReason { Normal, FirstRun, Restarted }

public static class Program
{
    public static StartupReason StartupReason { get; private set; } = StartupReason.Normal;
    public static string? StartupVersion { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must run before anything else — including WPF initialization.
        // During install/uninstall/update, this call handles the lifecycle event and exits.
        var hooks = new VelopackHooks(new WindowsRegistryReader());
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => hooks.OnInstall())
            .OnBeforeUninstallFastCallback(_ => hooks.OnUninstall())
            .OnFirstRun(v =>
            {
                StartupReason = StartupReason.FirstRun;
                StartupVersion = v.ToFullString();
            })
            .OnRestarted(v =>
            {
                StartupReason = StartupReason.Restarted;
                StartupVersion = v.ToFullString();
            })
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
