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

public class SettingsData
{
    [JsonPropertyName("startOnBoot")]
    public bool StartOnBoot { get; set; }

    [JsonPropertyName("showProfileToast")]
    public bool ShowProfileToast { get; set; } = true;

    [JsonPropertyName("minimizeToTray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("inputIntervalMs")]
    public int InputIntervalMs { get; set; } = 30;
}
