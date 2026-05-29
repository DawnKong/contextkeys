using System.Diagnostics;
using System.Text;
using ContextKeys.Models;
using ContextKeys.Utils;

namespace ContextKeys.Services;

public class WindowEnumerationService
{
    public List<WindowInfo> EnumVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        var selfTitle = Process.GetCurrentProcess().MainWindowTitle;

        Win32Api.EnumWindows((hWnd, _) =>
        {
            if (!Win32Api.IsWindowVisible(hWnd))
                return true;

            var title = GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(title))
                return true;

            // Filter out self
            if (title == selfTitle)
                return true;

            // Filter out desktop and taskbar
            var processName = GetProcessName(hWnd);
            if (processName is "explorer" or "SearchApp" or "ShellExperienceHost" or "ApplicationFrameHost")
            {
                var lowTitle = title.ToLowerInvariant();
                if (lowTitle.Contains("任务视图") || lowTitle.Contains("task view") ||
                    lowTitle == "program manager" || lowTitle.Contains("系统托盘") ||
                    lowTitle.Contains("notification") || lowTitle == "start")
                    return true;
            }

            windows.Add(new WindowInfo
            {
                Hwnd = hWnd,
                Title = title,
                ProcessName = processName,
                ProcessPath = GetProcessPath(hWnd)
            });

            return true;
        }, nint.Zero);

        return windows;
    }

    public WindowInfo? GetForegroundWindowInfo()
    {
        var hWnd = Win32Api.GetForegroundWindow();
        if (hWnd == nint.Zero)
            return null;

        var title = GetWindowTitle(hWnd);
        if (string.IsNullOrWhiteSpace(title))
            return null;

        return new WindowInfo
        {
            Hwnd = hWnd,
            Title = title,
            ProcessName = GetProcessName(hWnd),
            ProcessPath = GetProcessPath(hWnd)
        };
    }

    private static string GetWindowTitle(nint hWnd)
    {
        var length = Win32Api.GetWindowTextLength(hWnd);
        if (length == 0)
            return string.Empty;

        var sb = new StringBuilder(length + 1);
        Win32Api.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetProcessName(nint hWnd)
    {
        Win32Api.GetWindowThreadProcessId(hWnd, out var pid);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            return $"pid:{pid}";
        }
    }

    private static string GetProcessPath(nint hWnd)
    {
        Win32Api.GetWindowThreadProcessId(hWnd, out var pid);
        var hProcess = Win32Api.OpenProcess(Win32Api.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == nint.Zero)
            return string.Empty;

        try
        {
            var sb = new StringBuilder(1024);
            var size = sb.Capacity;
            if (Win32Api.QueryFullProcessImageName(hProcess, 0, sb, ref size) != 0)
                return sb.ToString();
            return string.Empty;
        }
        finally
        {
            Win32Api.CloseHandle(hProcess);
        }
    }
}
