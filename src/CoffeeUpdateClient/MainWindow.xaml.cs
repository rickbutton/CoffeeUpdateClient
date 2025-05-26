using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Utils;
using CoffeeUpdateClient.ViewModels;
using Microsoft.Win32;
using ReactiveUI;
using Serilog;

namespace CoffeeUpdateClient;

public partial class MainWindow
{
    public MainWindow(IConfigService configService, IAddOnDownloader addOnDownloader, LocalAddOnMetadataLoader localAddOnMetadataLoader)
    {
        InitializeComponent();
        ViewModel = new AppViewModel(configService, addOnDownloader, localAddOnMetadataLoader);

        this.WhenActivated(disposableRegistration =>
        {
#pragma warning disable CS8602
            this.WhenAnyValue(x => x.ViewModel.AddOnsPath)
                .CombineLatest(this.WhenAnyValue(x => x.ViewModel.NormalizedAddOnsPath))
#pragma warning restore CS8602
                .Select(x =>
                {
                    var (path, normalizedPath) = x;
                    return !string.IsNullOrEmpty(normalizedPath) ? normalizedPath : path;
                })
                .BindTo(this, view => view.WoWAddOnsFolderTextBox.Text)
                .DisposeWith(disposableRegistration);

            this.BindCommand(ViewModel,
                vm => vm.Browse,
                view => view.BrowseButton)
                .DisposeWith(disposableRegistration);

            this.OneWayBind(ViewModel,
                vm => vm.AddOnsPathState,
                view => view.UpdateButton.IsEnabled,
                value => value == AppViewModel.AddOnsPathStateEnum.Valid)
                .DisposeWith(disposableRegistration);

            this.OneWayBind(ViewModel,
                vm => vm.AddOnsPathState,
                view => view.ErrorMessageTextBlock.Text,
                value =>
                {
                    switch (value)
                    {
                        case AppViewModel.AddOnsPathStateEnum.Invalid:
                            return "WoW AddOns folder is invalid. Make sure you selected a valid folder for WoW.";
                        case AppViewModel.AddOnsPathStateEnum.Valid:
                            return "Valid WoW AddOn folder selected.";
                        case AppViewModel.AddOnsPathStateEnum.NotSet:
                            return string.Empty;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(value), value, null);
                    }
                })
                .DisposeWith(disposableRegistration);

            this.OneWayBind(ViewModel,
                vm => vm.AddOnsPathState,
                view => view.ErrorMessageTextBlock.Foreground,
                value =>
                {
                    switch (value)
                    {
                        case AppViewModel.AddOnsPathStateEnum.Invalid:
                            return Brushes.Red;
                        case AppViewModel.AddOnsPathStateEnum.Valid:
                            return Brushes.Green;
                        case AppViewModel.AddOnsPathStateEnum.NotSet:
                            return Brushes.Black;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(value), value, null);
                    }
                })
                .DisposeWith(disposableRegistration);

            this.OneWayBind(ViewModel,
                vm => vm.AddOns,
                view => view.AddOnListView.ItemsSource)
                .DisposeWith(disposableRegistration);

            VersionTextBlock.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version}";
        });
    }
}