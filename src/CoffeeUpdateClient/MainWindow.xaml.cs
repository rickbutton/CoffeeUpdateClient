using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        public bool CanUpdate => !IsWorkInProgress && PathState == AddOnsPathStateEnum.Valid;
        public string UpdateButtonText
        {
            get
            {
                if (IsWorkInProgress)
                {
                    return "Working";
                }
                else if (PathState != AddOnsPathStateEnum.Valid)
                {
                    return "Invalid Path";
                }
                else
                {
                    return "Update";
                }
            }
        }

        public State(string? addOnsPath)
        {
            ClientVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "vUNK";
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

    public MainWindow(AddOnUpdateManager updateManager, InstallLogCollector installLog)
    {
        CurrentState = new State(Config.Instance.AddOnsPath);
        _updateManager = updateManager;
        _installLog = installLog;

        this.DataContext = CurrentState;
        InitializeComponent();

        _installLog.LogCleared += OnLogCleared;
        _installLog.LogAdded += OnLogAdded;

        _ = LoadInitialStateAsync();
    }

    private void OnLogCleared()
    {
        CurrentState.ClearLog();
    }

    private void OnLogAdded(string message)
    {
        CurrentState.AddLog(message);
        LogTextBox.ScrollToEnd();
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

    private async void Update_Clicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await UpdateAddOnsAsync();
        }
        catch (Exception ex)
        {
            Log.Error("exception during update: {error}", ex);
            _installLog.AddLog($"An exception occured during update: {ex}");
        }
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

    private async Task LoadInitialStateAsync()
    {
        try
        {
            _installLog.ClearLog();
            CurrentState.IsWorkInProgress = true;
            if (!DisplayConfigState())
            {
                return;
            }

            var installStates = await _updateManager.GetAddOnInstallStatesForLatestManifestAsync(false);
            if (installStates == null)
            {
                _installLog.AddLog("Unable to determine current state of addons. See detailed log for more information.");
                return;
            }

            foreach (var state in installStates)
            {
                if (state.LocalAddOn == null)
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
        }
        catch (Exception ex)
        {
            Log.Error("exception while loading install state: {error}", ex);
            _installLog.AddLog($"An exception occured while loading install state: {ex}");
        }
        finally
        {
            CurrentState.IsWorkInProgress = false;
        }
    }

    private async Task UpdateAddOnsAsync()
    {
        try
        {
            _installLog.ClearLog();

            CurrentState.IsWorkInProgress = true;
            if (!DisplayConfigState())
            {
                return;
            }

            _installLog.AddLog("Starting AddOn Update...");
            var success = await _updateManager.UpdateAddOns(false);

            if (success)
            {
                _installLog.AddLog("AddOn install finished successfully");
            }
            else
            {
                _installLog.AddLog("AddOn install ended in an error. See detailed log for more information.");
            }
        }
        finally
        {
            CurrentState.IsWorkInProgress = false;
        }
    }
}