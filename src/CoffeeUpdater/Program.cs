using CoffeeUpdater.Services;
using CoffeeUpdater.Utils;
using Velopack;

namespace CoffeeUpdater;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must run before anything else — including WPF initialization.
        // During install/uninstall/update, this call handles the lifecycle event and exits.
        var hooks = new VelopackHooks(new WindowsRegistryReader());
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => hooks.OnInstall())
            .OnBeforeUninstallFastCallback(_ => hooks.OnUninstall())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
