using System.Text.Json.Serialization;

namespace ContextKeys.Models;

public class Profile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("match")]
    public WindowMatch Match { get; set; } = new();

    [JsonPropertyName("rules")]
    public List<HotkeyRule> Rules { get; set; } = new();
}
