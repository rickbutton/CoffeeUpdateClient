using CoffeeUpdater.Models;

namespace CoffeeUpdater.Tests;

[TestFixture]
public class ConfigTest
{
    [Test]
    public void SetAddOnsPath_FiresConfigUpdated()
    {
        var config = new Config();
        Config? received = null;
        config.ConfigUpdated += c => received = c;

        config.AddOnsPath = @"C:\new\path";

        Assert.That(received, Is.SameAs(config));
    }

    [Test]
    public void SetInstalledFolders_FiresConfigUpdated()
    {
        var config = new Config();
        Config? received = null;
        config.ConfigUpdated += c => received = c;

        config.SetInstalledFolders("MyAddOn", ["MyAddOn", "MyAddOn_Lib"]);

        Assert.That(received, Is.SameAs(config));
        Assert.That(config.InstalledAddOnFolders["MyAddOn"], Is.EquivalentTo(new[] { "MyAddOn", "MyAddOn_Lib" }));
    }

    [Test]
    public void RemoveInstalledFolders_FiresConfigUpdated()
    {
        var config = new Config();
        config.SetInstalledFolders("MyAddOn", ["MyAddOn"]);

        Config? received = null;
        config.ConfigUpdated += c => received = c;

        config.RemoveInstalledFolders("MyAddOn");

        Assert.That(received, Is.SameAs(config));
        Assert.That(config.InstalledAddOnFolders.ContainsKey("MyAddOn"), Is.False);
    }

    [Test]
    public void CopyConstructor_DeepCopiesAllFields()
    {
        var original = new Config { AddOnsPath = @"C:\path" };
        original.SetInstalledFolders("Addon", ["Addon", "Addon_Lib"]);

        var copy = new Config(original);

        Assert.That(copy.AddOnsPath, Is.EqualTo(original.AddOnsPath));
        Assert.That(copy.InstalledAddOnFolders["Addon"], Is.EquivalentTo(new[] { "Addon", "Addon_Lib" }));

        // Verify it's a deep copy — mutating original doesn't affect copy
        original.SetInstalledFolders("Addon", ["Addon"]);
        Assert.That(copy.InstalledAddOnFolders["Addon"], Is.EquivalentTo(new[] { "Addon", "Addon_Lib" }));
    }

    [Test]
    public void ConfigUpdated_NoSubscribers_DoesNotThrow()
    {
        var config = new Config();

        Assert.DoesNotThrow(() => config.AddOnsPath = "test");
        Assert.DoesNotThrow(() => config.SetInstalledFolders("a", ["a"]));
        Assert.DoesNotThrow(() => config.RemoveInstalledFolders("a"));
    }
}
