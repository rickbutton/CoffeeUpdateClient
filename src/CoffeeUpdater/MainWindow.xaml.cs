using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using CoffeeUpdater.Utils;
using Microsoft.Win32;
using Serilog;

namespace CoffeeUpdater;

public partial class MainWindow : INotifyPropertyChanged
{

    public enum SyncStatus
    {
        UpToDate,
        Syncing,
        Updated,
        Error,
        PathNotSet,
        PathInvalid,
    }

    private class State : INotifyPropertyChanged
    {
        private readonly Config _config;

        // real state
        public string ClientVersion { get; private set; }
        public string? AddOnsPath { get; private set; }
        public string? NormalizedAddOnsPath { get; private set; }
        public SyncStatus Status { get; set; } = SyncStatus.Syncing;
        public int UpdatedCount { get; set; }
        public string? ErrorMessage { get; set; }

        // derived state
        public string? SelectedAddOnsPath => string.IsNullOrEmpty(AddOnsPath) ? NormalizedAddOnsPath : AddOnsPath;

        public string StatusText => Status switch
        {
            SyncStatus.UpToDate => "All addons up to date",
            SyncStatus.Syncing => "Checking for updates\u2026",
            SyncStatus.Updated => $"Updated {UpdatedCount} addon{(UpdatedCount != 1 ? "s" : "")}",
            SyncStatus.Error => "Sync failed",
            SyncStatus.PathNotSet => "Set your WoW AddOns folder to get started",
            SyncStatus.PathInvalid => "Invalid AddOns path",
            _ => "",
        };

        public Brush StatusDotColor => Status switch
        {
            SyncStatus.UpToDate => Brushes.Green,
            SyncStatus.Syncing => Brushes.DodgerBlue,
            SyncStatus.Updated => Brushes.Green,
            SyncStatus.Error => Brushes.Red,
            SyncStatus.PathNotSet => Brushes.Orange,
            SyncStatus.PathInvalid => Brushes.Red,
            _ => Brushes.Gray,
        };

        public string? StatusDetail => Status switch
        {
            SyncStatus.Error => ErrorMessage,
            _ => null,
        };

        public Visibility StatusDetailVisibility =>
            string.IsNullOrEmpty(StatusDetail) ? Visibility.Collapsed : Visibility.Visible;

        public State(Config config)
        {
            _config = config;
            ClientVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "vUNK";
            AddOnsPath = config.AddOnsPath;
            NormalizedAddOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(config.AddOnsPath);

            if (string.IsNullOrEmpty(AddOnsPath))
                Status = SyncStatus.PathNotSet;
            else if (string.IsNullOrEmpty(NormalizedAddOnsPath))
                Status = SyncStatus.PathInvalid;
            else
                Status = SyncStatus.Syncing;
        }

        public void SelectPath(string path)
        {
            AddOnsPath = path;
            NormalizedAddOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(path);
            Log.Information("User selected path: {Path}", path);
            _config.AddOnsPath = NormalizedAddOnsPath ?? string.Empty;

            if (string.IsNullOrEmpty(NormalizedAddOnsPath))
                Status = SyncStatus.PathInvalid;
            else
                Status = SyncStatus.Syncing;
        }

        public bool HasValidPath => !string.IsNullOrEmpty(NormalizedAddOnsPath);

#pragma warning disable 67
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67
    }

    private State CurrentState { get; set; }

    private readonly AddOnSyncService _syncService;
    private System.Windows.Threading.DispatcherTimer? _revertTimer;

    public MainWindow(AddOnSyncService syncService, InstallLogCollector installLog, Config config)
    {
        CurrentState = new State(config);
        _syncService = syncService;

        this.DataContext = CurrentState;
        InitializeComponent();

        _syncService.SyncStarted += OnSyncStarted;
        _syncService.SyncCompleted += OnSyncCompleted;
        _syncService.SyncError += OnSyncError;
        installLog.UpdatesApplied += OnUpdatesApplied;

    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
        {
            // Ctrl+click the X button to fully exit the app
            Application.Current.Shutdown();
            return;
        }

        // Hide to tray instead of closing
        e.Cancel = true;
        Hide();
    }

    private void OnSyncStarted()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!CurrentState.HasValidPath) return;
            CurrentState.Status = SyncStatus.Syncing;
        });
    }

    private void OnSyncCompleted()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!CurrentState.HasValidPath) return;
            // Only revert to UpToDate if we're still in Syncing state
            // (Updated state will revert on its own timer)
            if (CurrentState.Status == SyncStatus.Syncing)
                CurrentState.Status = SyncStatus.UpToDate;
        });
    }

    private void OnSyncError(string message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            CurrentState.ErrorMessage = message;
            CurrentState.Status = SyncStatus.Error;
        });
    }

    private void OnUpdatesApplied(int count)
    {
        Dispatcher.BeginInvoke(() =>
        {
            CurrentState.UpdatedCount = count;
            CurrentState.Status = SyncStatus.Updated;

            // Revert to "up to date" after a few seconds
            _revertTimer?.Stop();
            _revertTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _revertTimer.Tick += (_, _) =>
            {
                _revertTimer.Stop();
                if (CurrentState.Status == SyncStatus.Updated)
                    CurrentState.Status = SyncStatus.UpToDate;
            };
            _revertTimer.Start();
        });
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

#pragma warning disable 67
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67
}
