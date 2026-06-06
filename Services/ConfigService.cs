using System.Text;
using System.Text.Json;
using System.Windows;
using ContextKeys.Models;

namespace ContextKeys.Services;

public class ConfigService
{
    private readonly string _configDir;
    private readonly string _configPath;
    private AppSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ConfigService()
    {
        _configDir = GetConfigDirectory();
        _configPath = Path.Combine(_configDir, "config.json");
        _settings = LoadInternal();
    }

    private static string GetConfigDirectory()
    {
        // 优先使用 AppData 目录
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ContextKeys");

        // 检查是否可写
        if (IsDirectoryWritable(appDataDir))
            return appDataDir;

        // 回退到程序目录
        var exeDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(exeDir))
        {
            var localDir = Path.Combine(exeDir, "config");
            try
            {
                Directory.CreateDirectory(localDir);
                if (IsDirectoryWritable(localDir))
                    return localDir;
            }
            catch
            {
                // 忽略错误，继续尝试其他方案
            }
        }

        // 最后的回退：临时目录
        return Path.Combine(Path.GetTempPath(), "ContextKeys");
    }

    private static bool IsDirectoryWritable(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var testFile = Path.Combine(path, Guid.NewGuid().ToString() + ".tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public AppSettings Settings => _settings;

    public string ConfigDirectory => _configDir;

    public event Action<AppSettings>? SettingsChanged;

    public void Save()
    {
        Directory.CreateDirectory(_configDir);
        var json = JsonSerializer.Serialize(_settings, JsonOptions);
        var tempPath = Path.Combine(_configDir, $"config.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempPath, json, Encoding.UTF8);
        try
        {
            if (File.Exists(_configPath))
                File.Replace(tempPath, _configPath, null, ignoreMetadataErrors: true);
            else
                File.Move(tempPath, _configPath);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        SettingsChanged?.Invoke(_settings);
    }

    private AppSettings LoadInternal()
    {
        try
        {
            if (!File.Exists(_configPath))
                return CreateDefault();

            var json = File.ReadAllText(_configPath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return Normalize(settings ?? CreateDefault());
        }
        catch
        {
            return CreateDefault();
        }
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        settings.Settings ??= new SettingsData();
        settings.Settings.InputIntervalMs = Math.Clamp(settings.Settings.InputIntervalMs, 1, 10_000);
        if (settings.Settings.ToastDisplayMode is not ("timed" or "always" or "hidden"))
            settings.Settings.ToastDisplayMode = "timed";
        settings.Profiles ??= new List<Profile>();

        foreach (var profile in settings.Profiles)
        {
            profile.Id = string.IsNullOrWhiteSpace(profile.Id) ? Guid.NewGuid().ToString("N") : profile.Id;
            profile.Name ??= string.Empty;
            profile.Match ??= new WindowMatch();
            profile.Rules ??= new List<HotkeyRule>();

            foreach (var rule in profile.Rules)
            {
                rule.Id = string.IsNullOrWhiteSpace(rule.Id) ? Guid.NewGuid().ToString("N") : rule.Id;
                rule.Name ??= string.Empty;
                rule.Hotkey ??= new HotkeyDefinition();
                rule.Hotkey.Key ??= string.Empty;
                rule.Hotkey.Modifiers ??= new List<string>();
                rule.Hotkey.Display ??= string.Empty;
                rule.Actions ??= new List<ActionStep>();

                foreach (var action in rule.Actions)
                {
                    action.Type = string.IsNullOrWhiteSpace(action.Type) ? "sequence" : action.Type;
                    action.Display ??= string.Empty;
                }
            }
        }

        return settings;
    }

    private AppSettings CreateDefault()
    {
        var settings = new AppSettings
        {
            Profiles = new List<Profile>
            {
                new()
                {
                    Id = "example_chatgpt",
                    Name = "ChatGPT 快捷语",
                    Enabled = false,
                    Match = new WindowMatch
                    {
                        ProcessName = "chrome",
                        TitleContains = "ChatGPT",
                        MatchMode = "process_and_title_contains"
                    },
                    Rules = new List<HotkeyRule>
                    {
                        new()
                        {
                            Id = "rule_continue",
                            Name = "继续",
                            Hotkey = new HotkeyDefinition
                            {
                                Key = "F8",
                                Modifiers = new List<string>(),
                                Display = "F8"
                            },
                            SuppressOriginalKey = true,
                            Actions = new List<ActionStep>
                            {
                                new()
                                {
                                    Type = "sequence",
                                    Keys = new List<string> { "J", "I", "X", "U", "Space", "Enter" },
                                    IntervalMs = 30,
                                    Display = "J → I → X → U → Space → Enter"
                                }
                            }
                        },
                        new()
                        {
                            Id = "rule_explain",
                            Name = "详细解释",
                            Hotkey = new HotkeyDefinition
                            {
                                Key = "F9",
                                Modifiers = new List<string>(),
                                Display = "F9"
                            },
                            SuppressOriginalKey = true,
                            Actions = new List<ActionStep>
                            {
                                new()
                                {
                                    Type = "sequence",
                                    Keys = new List<string> { "X", "I", "A", "N", "G", "X", "I", "J", "I", "E", "S", "H", "I", "Space", "Enter" },
                                    IntervalMs = 30,
                                    Display = "X → I → A → N → G → X → I → J → I → E → S → H → I → Space → Enter"
                                }
                            }
                        }
                    }
                }
            }
        };

        return settings;
    }
}
