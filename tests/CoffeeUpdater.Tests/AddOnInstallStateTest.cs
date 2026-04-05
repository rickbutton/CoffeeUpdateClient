using CoffeeUpdater.Models;
using NUnit.Framework;

namespace CoffeeUpdater.Tests;

public class AddOnInstallStateTest
{
    [Test]
    public void ShouldUninstall_NullVersion_ReturnsTrue()
    {
        var remote = new AddOnMetadata { Name = "Foo", Version = null };
        var state = new AddOnInstallState(null, remote);
        Assert.That(state.ShouldUninstall, Is.True);
    }

    [Test]
    public void ShouldUninstall_NonNullVersion_ReturnsFalse()
    {
        var remote = new AddOnMetadata { Name = "Foo", Version = "1.0.0" };
        var state = new AddOnInstallState(null, remote);
        Assert.That(state.ShouldUninstall, Is.False);
    }

    [Test]
    public void Constructor_MismatchedNames_ThrowsInvalidOperationException()
    {
        var local = new AddOnMetadata { Name = "Foo", Version = "1.0.0" };
        var remote = new AddOnMetadata { Name = "Bar", Version = "1.0.0" };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new AddOnInstallState(local, remote));

        Assert.That(ex?.Message, Does.Contain("Foo"));
        Assert.That(ex?.Message, Does.Contain("Bar"));
    }
}
