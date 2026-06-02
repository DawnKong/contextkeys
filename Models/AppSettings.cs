using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ContextKeys.Models;

public class AppSettings
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("settings")]
    public SettingsData Settings { get; set; } = new();

    [JsonPropertyName("profiles")]
    public List<Profile> Profiles { get; set; } = new();
}

public class SettingsData : INotifyPropertyChanged
{
    private bool _startOnBoot;
    private string _toastDisplayMode = "timed";
    private bool _minimizeToTray = true;
    private bool _paused;
    private int _inputIntervalMs = 30;

    [JsonPropertyName("startOnBoot")]
    public bool StartOnBoot
    {
        get => _startOnBoot;
        set { _startOnBoot = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("toastDisplayMode")]
    public string ToastDisplayMode
    {
        get => _toastDisplayMode;
        set { _toastDisplayMode = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set { _minimizeToTray = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("paused")]
    public bool Paused
    {
        get => _paused;
        set { _paused = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("inputIntervalMs")]
    public int InputIntervalMs
    {
        get => _inputIntervalMs;
        set { _inputIntervalMs = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
