using CoffeeUpdater.Utils;

namespace CoffeeUpdater.Tests;

[TestFixture]
public class RelayCommandTest
{
    [Test]
    public void Execute_InvokesAction()
    {
        var invoked = false;
        var command = new RelayCommand(() => invoked = true);

        command.Execute(null);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public void CanExecute_AlwaysReturnsTrue()
    {
        var command = new RelayCommand(() => { });

        Assert.That(command.CanExecute(null), Is.True);
        Assert.That(command.CanExecute("anything"), Is.True);
    }
}
