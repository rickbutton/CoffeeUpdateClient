using CoffeeUpdateClient.Models;
using NUnit.Framework;

namespace CoffeeUpdateClient.Tests;

public class AddOnInstallStateTest
{
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
