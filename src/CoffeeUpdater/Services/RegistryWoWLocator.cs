namespace CoffeeUpdater.Services;

public class RegistryWoWLocator : IWoWLocator
{
    private readonly IRegistryReader _registry;

    public RegistryWoWLocator(IRegistryReader registry)
    {
        _registry = registry;
    }

    public string? GetWoWInstallPath()
    {
        var installPath = FindApplicationPath(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "World of Warcraft");

        installPath ??= FindApplicationPath(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "World of Warcraft");

        return installPath;
    }

    private string? FindApplicationPath(string keyPath, string applicationName)
    {
        using var uninstall = _registry.OpenSubKey(keyPath);

        if (uninstall == null)
        {
            return null;
        }

        foreach (var productSubKey in uninstall.GetSubKeyNames())
        {
            using var product = uninstall.OpenSubKey(productSubKey);

            var displayName = product?.GetValue("DisplayName");
            if (displayName != null && displayName.ToString() == applicationName)
            {
                return product?.GetValue("InstallLocation")?.ToString();
            }
        }

        return null;
    }
}
