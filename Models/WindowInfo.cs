namespace ContextKeys.Models;

public class WindowInfo
{
    public nint Hwnd { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string ProcessPath { get; set; } = string.Empty;
    public string DisplayText => $"{Title} ({ProcessName})";
}
