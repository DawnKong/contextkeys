using System.Text.Json.Serialization;

namespace ContextKeys.Models;

public class WindowMatch
{
    [JsonPropertyName("processName")]
    public string ProcessName { get; set; } = string.Empty;

    [JsonPropertyName("processPath")]
    public string ProcessPath { get; set; } = string.Empty;

    [JsonPropertyName("titleContains")]
    public string TitleContains { get; set; } = string.Empty;

    [JsonPropertyName("titleEquals")]
    public string TitleEquals { get; set; } = string.Empty;

    [JsonPropertyName("matchMode")]
    public string MatchMode { get; set; } = "process_and_title_contains";
}
