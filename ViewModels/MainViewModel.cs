using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ContextKeys.Models;
using ContextKeys.Services;
using ContextKeys.Utils;

namespace ContextKeys.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _config;
    private readonly ForegroundWindowService _foreground;
    private readonly KeyboardHookService _keyboardHook;
    private readonly InputSimulationService _inputSim;
    private readonly ToastService _toast;

    private Profile? _currentProfile;
    private string _currentPage = "profiles";
    private string _currentProcessName = string.Empty;
    private string _currentTitle = string.Empty;
    private bool _paused;
    private string _lastTriggeredInfo = string.Empty;

    public MainViewModel()
    {
        _config = App.ConfigService;
        _foreground = App.ForegroundService;
        _keyboardHook = App.KeyboardHookService;
        _inputSim = App.InputSimService;
        _toast = App.ToastService;

        Profiles = new ObservableCollection<Profile>(_config.Settings.Profiles);
        _paused = _config.Settings.Settings.Paused;

        _foreground.ForegroundChanged += OnForegroundChanged;
        _keyboardHook.HotkeyTriggered += OnHotkeyTriggered;

        // Set initial profile
        var info = App.WindowEnumService.GetForegroundWindowInfo();
        if (info != null)
        {
            _currentProcessName = info.ProcessName;
            _currentTitle = info.Title;
            UpdateCurrentProfile();
        }

        _keyboardHook.SetPaused(_paused);
    }

    public ObservableCollection<Profile> Profiles { get; }

    public string CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public Profile? CurrentProfile
    {
        get => _currentProfile;
        set
        {
            _currentProfile = value;
            _keyboardHook.SetCurrentProfile(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentProfileName));
            OnPropertyChanged(nameof(HasCurrentProfile));
        }
    }

    public string CurrentProfileName => _currentProfile?.Name ?? "未匹配配置";
    public bool HasCurrentProfile => _currentProfile != null;
    public bool HasNoCurrentProfile => !HasCurrentProfile;

    public bool Paused
    {
        get => _paused;
        set
        {
            _paused = value;
            _config.Settings.Settings.Paused = value;
            _keyboardHook.SetPaused(value);
            _config.Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(PausedText));
        }
    }

    public string PausedText => _paused ? "已暂停" : "运行中";

    public string LastTriggeredInfo
    {
        get => _lastTriggeredInfo;
        set { _lastTriggeredInfo = value; OnPropertyChanged(); }
    }

    public Models.SettingsData Settings => _config.Settings.Settings;

    public string CurrentProcessName
    {
        get => _currentProcessName;
        set { _currentProcessName = value; OnPropertyChanged(); }
    }

    public string CurrentTitle
    {
        get => _currentTitle;
        set { _currentTitle = value; OnPropertyChanged(); }
    }

    public void NavigateTo(string page)
    {
        CurrentPage = page;
    }

    public void AddProfile(Profile profile)
    {
        Profiles.Add(profile);
        _config.Settings.Profiles = Profiles.ToList();
        _config.Save();
        // Immediately check if the new profile matches the current window
        UpdateCurrentProfile();
    }

    public void UpdateProfile(Profile oldProfile, Profile newProfile)
    {
        var index = Profiles.IndexOf(oldProfile);
        if (index >= 0)
        {
            Profiles[index] = newProfile;
            _config.Settings.Profiles = Profiles.ToList();
            _config.Save();

            // Re-check if current profile changed
            UpdateCurrentProfile();
        }
    }

    public void DeleteProfile(Profile profile)
    {
        Profiles.Remove(profile);
        _config.Settings.Profiles = Profiles.ToList();
        _config.Save();

        if (_currentProfile == profile)
        {
            CurrentProfile = null;
        }
    }

    public void ToggleProfile(Profile profile)
    {
        profile.Enabled = !profile.Enabled;
        _config.Save();
        UpdateCurrentProfile();
    }

    public void RefreshCurrentProfile()
    {
        UpdateCurrentProfile();
    }

    public void TestActions(List<ActionStep> actions)
    {
        SafeExecutionGuard.TryEnter();
        try
        {
            _inputSim.ExecuteActions(actions);
        }
        finally
        {
            SafeExecutionGuard.Exit();
        }
    }

    private void OnForegroundChanged(string processName, string title)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentProcessName = processName;
            CurrentTitle = title;
            UpdateCurrentProfile();
        });
    }

    private void UpdateCurrentProfile()
    {
        var matched = ProfileMatchService.Match(_currentProcessName, _currentTitle, Profiles.ToList());
        if (matched != _currentProfile)
        {
            CurrentProfile = matched;
            if (matched != null && _config.Settings.Settings.ShowProfileToast && !_paused)
            {
                _toast.ShowProfileToast(matched);
            }
        }
    }

    private void OnHotkeyTriggered(HotkeyRule rule)
    {
        // Enter the guard synchronously to prevent re-entry from simulated keys
        if (!SafeExecutionGuard.TryEnter())
        {
            LastTriggeredInfo = $"阻塞: {rule.Hotkey.Display} (执行中)";
            Logger.Warn($"OnHotkeyTriggered 阻塞: TryEnter 返回 false (已有动作在执行)");
            return;
        }

        var info = $"{DateTime.Now:HH:mm:ss} 触发: [{rule.Hotkey.Display}] {rule.Name}";
        TryUpdateUI(() => LastTriggeredInfo = info);
        Logger.Info($"OnHotkeyTriggered: {info}");

        // Execute in background to not block the hook
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _inputSim.ExecuteActions(rule.Actions, _config.Settings.Settings.InputIntervalMs);
                TryUpdateUI(() => LastTriggeredInfo = info + " ✓");
            }
            catch (Exception ex)
            {
                Logger.Error($"ExecuteActions 异常: {ex}");
                TryUpdateUI(() => LastTriggeredInfo = info + " ✗ " + ex.Message);
            }
            finally
            {
                SafeExecutionGuard.Exit();
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static void TryUpdateUI(Action action)
    {
        try
        {
            if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                Application.Current.Dispatcher.Invoke(action);
        }
        catch { /* app is shutting down */ }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
