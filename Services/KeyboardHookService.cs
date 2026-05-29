using System.Diagnostics;
using System.Runtime.InteropServices;
using ContextKeys.Models;
using ContextKeys.Utils;

namespace ContextKeys.Services;

public class KeyboardHookService : IDisposable
{
    private nint _hookId = nint.Zero;
    private readonly Win32Api.LowLevelKeyboardProc _hookProc;
    private Profile? _currentProfile;
    private bool _paused;
    private readonly Dictionary<string, List<HotkeyRule>> _cachedRules = new();

    // Debounce: prevent repeated triggers from key repeat
    private readonly Dictionary<string, DateTime> _lastTriggerTime = new();
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(300);

    // Pending trigger: matched on KEYDOWN, fires when ALL combo keys (modifiers + main) are released
    private HotkeyRule? _pendingRule;
    private HashSet<string>? _pendingComboKeys;       // All keys in the combo that need to go up
    private readonly HashSet<string> _releasedKeys = new(); // Keys from the combo that have gone up so far

    public KeyboardHookService()
    {
        _hookProc = HookCallback;
    }

    public event Action<HotkeyRule>? HotkeyTriggered;

    /// <summary>
    /// When set, matched hotkeys are passed to this interceptor instead of
    /// being executed normally. Used by RuleEditorDialog for test output.
    /// Return true to suppress normal execution.
    /// </summary>
    public static Func<string, HotkeyRule, bool>? TestInterceptor;

    public void Start()
    {
        _hookId = Win32Api.SetWindowsHookEx(
            Win32Api.WH_KEYBOARD_LL,
            _hookProc,
            Marshal.GetHINSTANCE(typeof(KeyboardHookService).Module),
            0);
    }

    public void Stop()
    {
        if (_hookId != nint.Zero)
        {
            Win32Api.UnhookWindowsHookEx(_hookId);
            _hookId = nint.Zero;
        }
    }

    public void SetCurrentProfile(Profile? profile)
    {
        _currentProfile = profile;
        CacheRules(profile);
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
    }

    private void CacheRules(Profile? profile)
    {
        _cachedRules.Clear();
        if (profile == null || !profile.Enabled)
            return;

        foreach (var rule in profile.Rules)
        {
            if (!rule.Enabled || rule.Hotkey.IsEmpty)
                continue;

            var key = BuildKeyString(rule.Hotkey);
            if (!_cachedRules.ContainsKey(key))
                _cachedRules[key] = new List<HotkeyRule>();
            _cachedRules[key].Add(rule);
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);

        // Skip if paused or no profile
        if (_paused || _currentProfile == null || !_currentProfile.Enabled)
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);

        var isKeyDown = wParam == (nint)Win32Api.WM_KEYDOWN || wParam == (nint)Win32Api.WM_SYSKEYDOWN;
        var isKeyUp = wParam == (nint)Win32Api.WM_KEYUP || wParam == (nint)Win32Api.WM_SYSKEYUP;

        var hookStruct = Marshal.PtrToStructure<Win32Api.KBDLLHOOKSTRUCT>(lParam);

        // Skip if injected by us
        if (hookStruct.dwExtraInfo == Win32Api.INJECTED_BY_APP)
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);

        var keyName = KeyCodeMapper.GetKeyName((byte)hookStruct.vkCode);

        // ── KEYUP: track released keys, fire when all combo keys are up ──
        if (isKeyUp && _pendingRule != null && _pendingComboKeys != null)
        {
            if (_pendingComboKeys.Contains(keyName))
            {
                _releasedKeys.Add(keyName);

                if (_releasedKeys.SetEquals(_pendingComboKeys))
                {
                    // All keys in the combo have been released — fire!
                    Logger.Info($"✓ 触发（所有键已松开）: {_pendingRule.Name} → {_pendingRule.Actions.Count} 个动作");
                    HotkeyTriggered?.Invoke(_pendingRule);
                    _pendingRule = null;
                    _pendingComboKeys = null;
                    _releasedKeys.Clear();
                }
            }
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── KEYDOWN: check for match, store as pending ──
        if (isKeyDown)
        {
            var modifiers = GetCurrentModifiers();
            var fullKey = BuildKeyString(keyName, modifiers);

            Logger.Info($"按键: {fullKey} | 缓存规则数: {_cachedRules.Count} | 当前配置: {_currentProfile?.Name}");

            // Test interceptor check
            if (TestInterceptor != null && TestInterceptor(fullKey, null!))
            {
                Logger.Info($"✓ 测试拦截器已处理: {fullKey}");
                return new nint(1);
            }

            if (_cachedRules.TryGetValue(fullKey, out var rules))
            {
                var now = DateTime.UtcNow;
                if (_lastTriggerTime.TryGetValue(fullKey, out var lastTime) &&
                    (now - lastTime) < DebounceInterval)
                {
                    return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }
                _lastTriggerTime[fullKey] = now;

                Logger.Info($"✓ 待触发（等待所有键松开）: {rules[0].Name}");

                // Store pending rule and all keys in the combo (modifiers + main key)
                _pendingRule = rules[0];
                _pendingComboKeys = new HashSet<string>(modifiers, StringComparer.OrdinalIgnoreCase)
                {
                    keyName
                };
                _releasedKeys.Clear();

                // Suppress the original key if needed
                if (rules[0].SuppressOriginalKey)
                    return new nint(1);
            }
        }

        return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static List<string> GetCurrentModifiers()
    {
        var mods = new List<string>();
        if (KeyModifierState(0x10))   mods.Add("Shift");
        if (KeyModifierState(0xA0))   mods.Add("LShift");
        if (KeyModifierState(0xA1))   mods.Add("RShift");
        if (KeyModifierState(0x11))   mods.Add("Ctrl");
        if (KeyModifierState(0xA2))   mods.Add("LCtrl");
        if (KeyModifierState(0xA3))   mods.Add("RCtrl");
        if (KeyModifierState(0x12))   mods.Add("Alt");
        if (KeyModifierState(0xA4))   mods.Add("LAlt");
        if (KeyModifierState(0xA5))   mods.Add("RAlt");
        if (KeyModifierState(0x5B))   mods.Add("LWin");
        if (KeyModifierState(0x5C))   mods.Add("RWin");
        return mods;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool KeyModifierState(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }

    private static string BuildKeyString(string key, List<string>? modifiers = null)
    {
        if (modifiers == null || modifiers.Count == 0)
            return key;
        // Sort modifiers to ensure canonical order, regardless of detection order
        var sorted = modifiers.OrderBy(m => m).ToList();
        return string.Join("+", sorted) + "+" + key;
    }

    private static string BuildKeyString(HotkeyDefinition hk)
    {
        if (hk.Modifiers.Count == 0)
            return hk.Key;
        // Sort modifiers to ensure canonical order, matching the hook detection side
        var sorted = hk.Modifiers.OrderBy(m => m).ToList();
        return string.Join("+", sorted) + "+" + hk.Key;
    }

    public void Dispose()
    {
        Stop();
    }
}
