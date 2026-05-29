using System.Text.Json.Serialization;

namespace ContextKeys.Models;

public class HotkeyRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("hotkey")]
    public HotkeyDefinition Hotkey { get; set; } = new();

    [JsonPropertyName("suppressOriginalKey")]
    public bool SuppressOriginalKey { get; set; } = true;

    [JsonPropertyName("actions")]
    public List<ActionStep> Actions { get; set; } = new();
}
