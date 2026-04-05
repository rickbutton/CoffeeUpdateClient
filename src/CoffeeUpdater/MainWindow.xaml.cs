using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using CoffeeUpdater.Utils;
using Microsoft.Win32;
using Serilog;

namespace CoffeeUpdater;

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

        private readonly Config _config;

        // real state
        public string ClientVersion { get; private set; }
        public string? AddOnsPath { get; private set; }
        public string? NormalizedAddOnsPath { get; private set; }
        public bool UserHasSelectedPath { get; private set; }
        public bool IsWorkInProgress { get; set; }

        public List<string> InstallLogs { get; private set; } = [];

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
        public string AllInstallLogs => string.Join("\n", InstallLogs);
        public string StatusText
        {
            get
            {
                if (IsWorkInProgress) return "Syncing...";
                if (PathState != AddOnsPathStateEnum.Valid) return "Invalid Path";
                return "Running";
            }
        }

        public State(Config config)
        {
            _config = config;
            ClientVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "vUNK";
            AddOnsPath = config.AddOnsPath;
            NormalizedAddOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(config.AddOnsPath);
            UserHasSelectedPath = false;
        }

        public void SelectPath(string path)
        {
            AddOnsPath = path;
            NormalizedAddOnsPath = AddOnPathResolver.NormalizeAddOnsDirectory(path);
            UserHasSelectedPath = true;
            Log.Information("User selected path: {Path}", path);
            _config.AddOnsPath = NormalizedAddOnsPath ?? string.Empty;
        }

        public void ClearLog()
        {
            InstallLogs = [];
        }

        public void AddLog(string message)
        {
            InstallLogs.Add(message);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstallLogs)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllInstallLogs)));
        }


#pragma warning disable 67
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67
    }

    private State CurrentState { get; set; }

    private readonly AddOnUpdateManager _updateManager;
    private readonly InstallLogCollector _installLog;

    public MainWindow(AddOnUpdateManager updateManager, InstallLogCollector installLog, Config config)
    {
        CurrentState = new State(config);
        _updateManager = updateManager;
        _installLog = installLog;

        this.DataContext = CurrentState;
        InitializeComponent();

        _installLog.LogCleared += OnLogCleared;
        _installLog.LogAdded += OnLogAdded;

        _ = LoadInitialStateAsync();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            // Ctrl+click the X button to fully exit the app
            Application.Current.Shutdown();
            return;
        }

        // Hide to tray instead of closing
        e.Cancel = true;
        Hide();
    }

    private void OnLogCleared()
    {
        Dispatcher.BeginInvoke(() => CurrentState.ClearLog());
    }

    private void OnLogAdded(string message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            CurrentState.AddLog(message);
            LogTextBox.ScrollToEnd();
        });
    }

    private async void Browse_Clicked(object sender, RoutedEventArgs e)
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

        await LoadInitialStateAsync();
    }

    private bool DisplayConfigState()
    {
        if (CurrentState.PathState == State.AddOnsPathStateEnum.Invalid)
        {
            _installLog.AddLog("AddOns path is invalid. Set a proper path to the WoW AddOns folder");
            return false;
        }
        else if (CurrentState.PathState == State.AddOnsPathStateEnum.NotSet)
        {
            _installLog.AddLog("AddOns path is not set. Set a proper path to the WoW AddOns folder");
            return false;
        }
        return true;
    }

    private async Task RunWithProgress(string errorContext, Func<Task> action)
    {
        _installLog.ClearLog();
        CurrentState.IsWorkInProgress = true;
        try
        {
            if (!DisplayConfigState()) return;
            await action();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception {Context}", errorContext);
            _installLog.AddLog($"An exception occurred {errorContext}: {ex.Message}");
        }
        finally
        {
            CurrentState.IsWorkInProgress = false;
        }
    }

    private Task LoadInitialStateAsync() => RunWithProgress("while loading install state", async () =>
    {
        var installStates = await _updateManager.GetAddOnInstallStatesForLatestManifestAsync();
        if (installStates == null)
        {
            _installLog.AddLog("Unable to determine current state of addons. See detailed log for more information.");
            return;
        }

        foreach (var state in installStates)
        {
            if (state.ShouldUninstall)
            {
                if (state.IsInstalled)
                {
                    _installLog.AddLog($"- {state.Name} will be removed, local={state.LocalAddOn!.Version}");
                }
            }
            else if (state.HasLocalError)
            {
                _installLog.AddLog($"- {state.Name} has a local TOC error (version unreadable), remote={state.RemoteAddOn.Version}");
            }
            else if (state.LocalAddOn == null)
            {
                _installLog.AddLog($"- {state.Name} needs to be installed, remote={state.RemoteAddOn.Version}");
            }
            else if (!state.IsUpdated)
            {
                _installLog.AddLog($"- {state.Name} needs to be updated, local={state.LocalAddOn.Version} remote={state.RemoteAddOn.Version}");
            }
            else
            {
                _installLog.AddLog($"- {state.Name} is up to date, local={state.LocalAddOn.Version}");
            }
        }
    });

}
