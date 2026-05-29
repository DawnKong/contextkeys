using System.Text;
using System.Text.Json;
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
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ContextKeys");
        _configPath = Path.Combine(_configDir, "config.json");
        _settings = LoadInternal();
    }

    public AppSettings Settings => _settings;

    public event Action<AppSettings>? SettingsChanged;

    public void Save()
    {
        Directory.CreateDirectory(_configDir);
        var json = JsonSerializer.Serialize(_settings, JsonOptions);
        File.WriteAllText(_configPath, json, Encoding.UTF8);
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
            return settings ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
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

    public string GetConfigDirectory() => _configDir;
}
