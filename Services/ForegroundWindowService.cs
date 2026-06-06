using System.ComponentModel;
using System.Runtime.InteropServices;
using ContextKeys.Utils;

namespace ContextKeys.Services;

public class ForegroundWindowService : IDisposable
{
    private nint _hook;
    private readonly WindowEnumerationService _windowEnumService;
    private readonly Win32Api.WinEventDelegate _winEventDelegate;
    private string _currentProcessName = string.Empty;
    private string _currentTitle = string.Empty;

    public ForegroundWindowService()
    {
        _windowEnumService = new WindowEnumerationService();
        _winEventDelegate = OnForegroundChanged;
    }

    public event Action<string, string>? ForegroundChanged; // processName, title

    public void Start()
    {
        _hook = Win32Api.SetWinEventHook(
            Win32Api.EVENT_SYSTEM_FOREGROUND,
            Win32Api.EVENT_SYSTEM_FOREGROUND,
            nint.Zero,
            _winEventDelegate,
            0, 0,
            Win32Api.WINEVENT_OUTOFCONTEXT);

        if (_hook == nint.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "前台窗口监听安装失败");

        // Fire initial event
        var info = _windowEnumService.GetForegroundWindowInfo();
        if (info != null)
        {
            _currentProcessName = info.ProcessName;
            _currentTitle = info.Title;
        }
    }

    public (string processName, string title) GetCurrentForeground()
    {
        return (_currentProcessName, _currentTitle);
    }

    public void Stop()
    {
        if (_hook != nint.Zero)
        {
            Win32Api.UnhookWinEvent(_hook);
            _hook = nint.Zero;
        }
    }

    private void OnForegroundChanged(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == nint.Zero)
            return;

        var info = _windowEnumService.GetForegroundWindowInfo();
        if (info == null)
            return;

        _currentProcessName = info.ProcessName;
        _currentTitle = info.Title;
        ForegroundChanged?.Invoke(_currentProcessName, _currentTitle);
    }

    public void Dispose()
    {
        Stop();
    }
}
