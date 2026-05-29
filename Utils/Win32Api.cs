using System.Runtime.InteropServices;
using System.Text;

namespace ContextKeys.Utils;

public static class Win32Api
{
    // Window enumeration
    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int EnumChildWindows(nint hWndParent, EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    // Foreground window hook
    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc,
        WinEventDelegate lpfnWinEvent, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWinEvent(nint hWinEventHook);

    public delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    // Keyboard hook
    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    public delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP = 0x0105;

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }

    // SendInput
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // keybd_event — simpler API, no struct size issues
    [DllImport("user32.dll", SetLastError = true)]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nint dwExtraInfo);

    public const uint KEYEVENTF_KEYDOWN_LEGACY = 0x0000;
    public const uint KEYEVENTF_KEYUP_LEGACY = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint GetMessageExtraInfo();

    /// <summary>
    /// Returns the correct cbSize for INPUT struct. Must match the full Windows INPUT
    /// size including the largest union member (MOUSEINPUT on x64).
    /// On x64: 40 bytes. On x86: 28 bytes.
    /// </summary>
    public static int InputCbSize => IntPtr.Size == 8 ? 40 : 28;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    public const uint INPUT_KEYBOARD = 1;
    public const uint INPUT_MOUSE = 0;
    public const uint INPUT_HARDWARE = 2;
    public const uint KEYEVENTF_KEYDOWN = 0x0000;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_SCANCODE = 0x0008;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    // Custom extra info marker to identify our own input
    public static readonly nint INJECTED_BY_APP = new(0x51504B01);

    // VK key codes
    public const byte VK_CAPITAL = 0x14;
    public const byte VK_NUMLOCK = 0x90;
    public const byte VK_SCROLL = 0x91;
    public const byte VK_LSHIFT = 0xA0;
    public const byte VK_RSHIFT = 0xA1;
    public const byte VK_LCONTROL = 0xA2;
    public const byte VK_RCONTROL = 0xA3;
    public const byte VK_LMENU = 0xA4;
    public const byte VK_RMENU = 0xA5;
    public const byte VK_LWIN = 0x5B;
    public const byte VK_RWIN = 0x5C;

    // Process
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int QueryFullProcessImageName(nint hProcess, uint dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    /// <summary>
    /// Load a PNG file as a System.Drawing.Icon for use with NotifyIcon.
    /// The caller must dispose the returned Icon.
    /// </summary>
    public static System.Drawing.Icon LoadPngAsIcon(string pngPath)
    {
        using var bmp = new System.Drawing.Bitmap(pngPath);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }
}
