using CoffeeUpdateClient.Utils;
using NUnit.Framework;

namespace CoffeeUpdateClient.Tests;

public class InstallLogCollectorTest
{
    [Test]
    public void ClearLog_NoSubscribers_DoesNotThrow()
    {
        var log = new InstallLogCollector();
        Assert.DoesNotThrow(() => log.ClearLog());
    }

    [Test]
    public void ClearLog_WithSubscriber_FiresEvent()
    {
        var log = new InstallLogCollector();
        var fired = false;
        log.LogCleared += () => fired = true;

        log.ClearLog();

        Assert.That(fired, Is.True);
    }

    [Test]
    public void AddLog_NoSubscribers_DoesNotThrow()
    {
        var log = new InstallLogCollector();
        Assert.DoesNotThrow(() => log.AddLog("test message"));
    }

    [Test]
    public void AddLog_WithSubscriber_FiresEventWithCorrectMessage()
    {
        var log = new InstallLogCollector();
        string? received = null;
        log.LogAdded += message => received = message;

        log.AddLog("hello");

        Assert.That(received, Is.EqualTo("hello"));
    }
}
