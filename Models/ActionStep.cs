using System.Text.Json.Serialization;

namespace ContextKeys.Models;

public class ActionStep
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "sequence";

    [JsonPropertyName("keys")]
    public List<string>? Keys { get; set; }

    [JsonPropertyName("intervalMs")]
    public int IntervalMs { get; set; } = 30;

    [JsonPropertyName("milliseconds")]
    public int Milliseconds { get; set; }

    [JsonPropertyName("display")]
    public string Display { get; set; } = string.Empty;
}
