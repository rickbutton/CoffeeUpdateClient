using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace CoffeeUpdater.Utils;

public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute;
    }

    [ExcludeFromCodeCoverage]
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
