using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Tests.Mocks;
using NUnit.Framework;

namespace CoffeeUpdateClient.Tests;

public class RegistryWoWLocatorTest
{
    private const string PrimaryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string Wow64Key = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string WoWDisplayName = "World of Warcraft";
    private const string WoWInstallPath = @"C:\Program Files (x86)\World of Warcraft";

    [Test]
    public void GetWoWInstallPath_WoWFoundInPrimaryKey_ReturnsInstallPath()
    {
        var mockRegistry = new MockRegistryReader();
        var wowEntry = mockRegistry.AddKey(PrimaryKey).AddSubKey("Blizzard Entertainment WoW");
        wowEntry.SetValue("DisplayName", WoWDisplayName);
        wowEntry.SetValue("InstallLocation", WoWInstallPath);

        var locator = new RegistryWoWLocator(mockRegistry);

        Assert.That(locator.GetWoWInstallPath(), Is.EqualTo(WoWInstallPath));
    }

    [Test]
    public void GetWoWInstallPath_PrimaryKeyMissing_FallsBackToWoW64Key()
    {
        var mockRegistry = new MockRegistryReader();
        // Primary key not added — OpenSubKey returns null
        var wowEntry = mockRegistry.AddKey(Wow64Key).AddSubKey("Blizzard Entertainment WoW");
        wowEntry.SetValue("DisplayName", WoWDisplayName);
        wowEntry.SetValue("InstallLocation", WoWInstallPath);

        var locator = new RegistryWoWLocator(mockRegistry);

        Assert.That(locator.GetWoWInstallPath(), Is.EqualTo(WoWInstallPath));
    }

    [Test]
    public void GetWoWInstallPath_BothKeysMissing_ReturnsNull()
    {
        var mockRegistry = new MockRegistryReader();

        var locator = new RegistryWoWLocator(mockRegistry);

        Assert.That(locator.GetWoWInstallPath(), Is.Null);
    }

    [Test]
    public void GetWoWInstallPath_NoMatchingDisplayName_ReturnsNull()
    {
        var mockRegistry = new MockRegistryReader();
        var otherEntry = mockRegistry.AddKey(PrimaryKey).AddSubKey("SomeOtherApp");
        otherEntry.SetValue("DisplayName", "Some Other Application");
        otherEntry.SetValue("InstallLocation", @"C:\Program Files\SomeApp");

        var locator = new RegistryWoWLocator(mockRegistry);

        Assert.That(locator.GetWoWInstallPath(), Is.Null);
    }

    [Test]
    public void GetWoWInstallPath_ProductSubKeyIsNull_SkipsEntry()
    {
        var mockRegistry = new MockRegistryReader();
        mockRegistry.AddKey(PrimaryKey).AddNullSubKey("SomeEntry");

        var locator = new RegistryWoWLocator(mockRegistry);

        Assert.That(locator.GetWoWInstallPath(), Is.Null);
    }

    [Test]
    public void GetWoWInstallPath_InstallLocationIsNull_ReturnsNull()
    {
        var mockRegistry = new MockRegistryReader();
        var wowEntry = mockRegistry.AddKey(PrimaryKey).AddSubKey("Blizzard Entertainment WoW");
        wowEntry.SetValue("DisplayName", WoWDisplayName);
        // InstallLocation not set — GetValue returns null

        var locator = new RegistryWoWLocator(mockRegistry);

        Assert.That(locator.GetWoWInstallPath(), Is.Null);
    }
}
