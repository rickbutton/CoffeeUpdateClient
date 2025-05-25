using Microsoft.Win32;

namespace CoffeeUpdateClient.Services;

class RegistryWoWLocator : IWoWLocator
{
    public string? GetWoWInstallPath()
    {
        var installPath = FindApplicationPath(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "World of Warcraft");

        installPath ??= FindApplicationPath(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "World of Warcraft");

        return installPath;
    }

    private static string? FindApplicationPath(string keyPath, string applicationName)
    {
        var hklm = Registry.LocalMachine;
        var uninstall = hklm.OpenSubKey(keyPath);

        if (uninstall == null)
        {
            return null;
        }

        foreach (var productSubKey in uninstall.GetSubKeyNames())
        {
            var product = uninstall.OpenSubKey(productSubKey);

            var displayName = product?.GetValue("DisplayName");
            if (displayName != null && displayName.ToString() == applicationName)
            {
                var value = product?.GetValue("InstallLocation");
                if (value != null)
                {
                    return value.ToString();
                }
            }

        }

        return null;
    }
}