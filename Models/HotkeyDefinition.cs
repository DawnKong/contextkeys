using System.Text.Json.Serialization;

namespace ContextKeys.Models;

public class HotkeyDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = new();

    [JsonPropertyName("display")]
    public string Display { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsEmpty => string.IsNullOrEmpty(Key);
}
