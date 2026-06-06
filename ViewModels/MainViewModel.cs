using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using ContextKeys.Models;
using ContextKeys.Services;
using ContextKeys.Utils;
using Microsoft.Win32;

namespace ContextKeys.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private const string StartupRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRegistryValueName = "ContextKeys";

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

        _config.Settings.Settings.PropertyChanged += OnSettingsPropertyChanged;
        ApplyStartOnBootSetting(_config.Settings.Settings.StartOnBoot);

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
            OnPropertyChanged(nameof(PauseButtonText));
        }
    }

    public string PausedText => _paused ? "已暂停" : "运行中";
    public string PauseButtonText => _paused ? "▶ 恢复快捷键" : "暂停所有快捷键";

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
        if (!SafeExecutionGuard.TryEnter())
        {
            LastTriggeredInfo = "测试输出被阻塞：已有动作正在执行";
            Logger.Warn("TestActions 阻塞: TryEnter 返回 false (已有动作在执行)");
            return;
        }

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _inputSim.ExecuteActions(actions);
            }
            catch (Exception ex)
            {
                Logger.Error($"TestActions 执行异常: {ex}");
            }
            finally
            {
                SafeExecutionGuard.Exit();
            }
        });
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
            if (matched != null && !_paused)
            {
                var displayMode = _config.Settings.Settings.ToastDisplayMode;
                _toast.ShowProfileToast(matched, displayMode);
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
                var action = rule.Actions.FirstOrDefault();
                if (action == null)
                {
                    Logger.Warn($"规则没有输出动作: {rule.Name}");
                    return;
                }

                _inputSim.ExecuteActions(new List<ActionStep> { action }, _config.Settings.Settings.InputIntervalMs);
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

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsData.StartOnBoot))
            ApplyStartOnBootSetting(_config.Settings.Settings.StartOnBoot);

        _config.Save();
    }

    private static void ApplyStartOnBootSetting(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(StartupRegistryKeyPath);
            if (key == null)
                return;

            if (enabled)
            {
                key.SetValue(StartupRegistryValueName, BuildStartupCommand(), RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(StartupRegistryValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"开机自启动设置失败: {ex.Message}");
        }
    }

    private static string BuildStartupCommand()
    {
        var appExePath = Path.Combine(AppContext.BaseDirectory, "ContextKeys.exe");
        if (File.Exists(appExePath))
            return Quote(appExePath);

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var assemblyPath = Environment.ProcessPath;
            if (processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                assemblyPath = Path.Combine(AppContext.BaseDirectory, "ContextKeys.dll");
                return $"{Quote(processPath)} {Quote(assemblyPath)}";
            }

            return Quote(processPath);
        }

        return Quote(Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppContext.BaseDirectory, "ContextKeys.exe"));
    }

    private static string Quote(string value) => $"\"{value}\"";

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
