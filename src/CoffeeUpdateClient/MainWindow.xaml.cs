using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using CoffeeUpdateClient.Utils;
using Microsoft.Win32;
using Serilog;
using Windows.ApplicationModel.VoiceCommands;

namespace CoffeeUpdateClient;

public partial class MainWindow : INotifyPropertyChanged
{

    private class State : INotifyPropertyChanged
    {
        public enum AddOnsPathStateEnum
        {
            Invalid,
            Valid,
            NotSet,
        };

        // real state
        public string? AddOnsPath { get; private set; }
        public string? NormalizedAddOnsPath { get; private set; }
        public bool UserHasSelectedPath { get; private set; }

        // derived state
        public AddOnsPathStateEnum PathState
        {
            get
            {
                if (string.IsNullOrEmpty(AddOnsPath))
                {
                    return AddOnsPathStateEnum.NotSet;
                }
                else if (string.IsNullOrEmpty(NormalizedAddOnsPath))
                {
                    return AddOnsPathStateEnum.Invalid;
                }
                else
                {
                    return AddOnsPathStateEnum.Valid;
                }
            }
        }
        public string? SelectedAddOnsPath => string.IsNullOrEmpty(AddOnsPath) ? NormalizedAddOnsPath : AddOnsPath;

        public string ErrorMessage
        {
            get
            {
                return PathState switch
                {
                    AddOnsPathStateEnum.Invalid => "WoW AddOns folder is invalid. Make sure you selected a valid folder for WoW.",
                    AddOnsPathStateEnum.Valid => "Valid WoW AddOn folder selected.",
                    AddOnsPathStateEnum.NotSet => string.Empty,
                    _ => throw new ArgumentOutOfRangeException(nameof(PathState), PathState, null),
                };
            }
        }
        public SolidColorBrush ErrorMessageForeground
        {
            get
            {
                return PathState switch
                {
                    AddOnsPathStateEnum.Invalid => Brushes.Red,
                    AddOnsPathStateEnum.Valid => Brushes.Green,
                    AddOnsPathStateEnum.NotSet => Brushes.Black,
                    _ => throw new ArgumentOutOfRangeException(nameof(PathState), PathState, null),
                };
            }
        }

        public State(string? addOnsPath)
        {
            AddOnsPath = addOnsPath;
            NormalizedAddOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(addOnsPath);
            UserHasSelectedPath = false;
        }

        public void SelectPath(string path)
        {
            AddOnsPath = path;
            NormalizedAddOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(path);
            UserHasSelectedPath = true;
            Log.Information("User selected path: {Path}", path);
            Config.Instance.AddOnsPath = NormalizedAddOnsPath ?? string.Empty;
        }


#pragma warning disable 67
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67
    }

    private State CurrentState { get; set; }

    public MainWindow(IAddOnDownloader addOnDownloader, LocalAddOnMetadataLoader localAddOnMetadataLoader, AddOnBundleInstaller AddOnBundleInstaller)
    {
        CurrentState = new State(Config.Instance.AddOnsPath);
        this.DataContext = CurrentState;
        InitializeComponent();
    }

    private void Browse_Clicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (!string.IsNullOrEmpty(CurrentState.NormalizedAddOnsPath))
        {
            dialog.InitialDirectory = CurrentState.NormalizedAddOnsPath;
        }

        if (dialog.ShowDialog() == true)
        {
            CurrentState.SelectPath(dialog.FolderName);
        }
    }
}