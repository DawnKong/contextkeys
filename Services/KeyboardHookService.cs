using System.Diagnostics;
using System.ComponentModel;
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

    public void Start()
    {
        _hookId = Win32Api.SetWindowsHookEx(
            Win32Api.WH_KEYBOARD_LL,
            _hookProc,
            Win32Api.GetModuleHandle(null),
            0);

        if (_hookId == nint.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "低级键盘钩子安装失败");
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
        ClearPendingTrigger();
        CacheRules(profile);
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (paused)
            ClearPendingTrigger();
    }

    private void CacheRules(Profile? profile)
    {
        _cachedRules.Clear();
        if (profile == null || !profile.Enabled)
            return;

        foreach (var rule in profile.Rules)
        {
            if (!rule.Enabled || rule.Hotkey.IsEmpty || rule.Actions.Count == 0)
                continue;

            foreach (var key in BuildCacheKeys(rule.Hotkey))
            {
                if (!_cachedRules.ContainsKey(key))
                    _cachedRules[key] = new List<HotkeyRule>();
                _cachedRules[key].Add(rule);
            }
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);

        // Skip if paused, no profile, or an action is currently injecting keys.
        if (_paused || SafeExecutionGuard.IsExecuting || _currentProfile == null || !_currentProfile.Enabled)
        {
            ClearPendingTrigger();
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        var isKeyDown = wParam == (nint)Win32Api.WM_KEYDOWN || wParam == (nint)Win32Api.WM_SYSKEYDOWN;
        var isKeyUp = wParam == (nint)Win32Api.WM_KEYUP || wParam == (nint)Win32Api.WM_SYSKEYUP;

        var hookStruct = Marshal.PtrToStructure<Win32Api.KBDLLHOOKSTRUCT>(lParam);

        // Skip if injected by us
        if (hookStruct.dwExtraInfo == Win32Api.INJECTED_BY_APP)
            return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);

        var keyName = KeyCodeMapper.GetKeyName((byte)hookStruct.vkCode, (hookStruct.flags & Win32Api.LLKHF_EXTENDED) != 0);

        // ── KEYUP: track released keys, fire when all combo keys are up ──
        if (isKeyUp && _pendingRule != null && _pendingComboKeys != null)
        {
            var shouldSuppressKeyUp = _pendingRule.SuppressOriginalKey
                && string.Equals(keyName, _pendingRule.Hotkey.Key, StringComparison.OrdinalIgnoreCase);

            if (_pendingComboKeys.Contains(keyName))
            {
                _releasedKeys.Add(keyName);

                if (_releasedKeys.SetEquals(_pendingComboKeys))
                {
                    // All keys in the combo have been released — fire!
                    var rule = _pendingRule;
                    ClearPendingTrigger();
                    HotkeyTriggered?.Invoke(rule);
                }
            }

            return shouldSuppressKeyUp
                ? new nint(1)
                : Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── KEYDOWN: check for match, store as pending ──
        if (isKeyDown)
        {
            var modifiers = GetCurrentModifiers();
            var fullKey = BuildKeyString(keyName, modifiers);

            if (_cachedRules.TryGetValue(fullKey, out var rules))
            {
                var now = DateTime.UtcNow;
                if (_lastTriggerTime.TryGetValue(fullKey, out var lastTime) &&
                    (now - lastTime) < DebounceInterval)
                {
                    return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }
                _lastTriggerTime[fullKey] = now;

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
        AddModifierState(mods, 0xA0, 0xA1, 0x10, "LShift", "RShift", "Shift");
        AddModifierState(mods, 0xA2, 0xA3, 0x11, "LCtrl", "RCtrl", "Ctrl");
        AddModifierState(mods, 0xA4, 0xA5, 0x12, "LAlt", "RAlt", "Alt");
        AddModifierState(mods, 0x5B, 0x5C, null, "LWin", "RWin", "Win");
        return mods;
    }

    private static void AddModifierState(
        List<string> modifiers,
        int leftVk,
        int rightVk,
        int? genericVk,
        string leftName,
        string rightName,
        string genericName)
    {
        var hasLeft = KeyModifierState(leftVk);
        var hasRight = KeyModifierState(rightVk);

        if (hasLeft)
            modifiers.Add(leftName);
        if (hasRight)
            modifiers.Add(rightName);

        if (!hasLeft && !hasRight && genericVk.HasValue && KeyModifierState(genericVk.Value))
            modifiers.Add(genericName);
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

    private static IEnumerable<string> BuildCacheKeys(HotkeyDefinition hotkey)
    {
        var modifierVariants = new List<List<string>>();
        foreach (var modifier in hotkey.Modifiers)
            modifierVariants.Add(GetModifierAliases(modifier));

        if (modifierVariants.Count == 0)
        {
            yield return hotkey.Key;
            yield break;
        }

        foreach (var modifiers in ExpandModifierAliases(modifierVariants, 0, new List<string>()))
            yield return BuildKeyString(hotkey.Key, modifiers);
    }

    private static IEnumerable<List<string>> ExpandModifierAliases(List<List<string>> modifierVariants, int index, List<string> current)
    {
        if (index >= modifierVariants.Count)
        {
            yield return new List<string>(current);
            yield break;
        }

        foreach (var modifier in modifierVariants[index])
        {
            current.Add(modifier);
            foreach (var result in ExpandModifierAliases(modifierVariants, index + 1, current))
                yield return result;
            current.RemoveAt(current.Count - 1);
        }
    }

    private static List<string> GetModifierAliases(string modifier)
    {
        return modifier switch
        {
            "Ctrl" or "Control" => new List<string> { "Ctrl", "LCtrl", "RCtrl" },
            "Shift" => new List<string> { "Shift", "LShift", "RShift" },
            "Alt" => new List<string> { "Alt", "LAlt", "RAlt" },
            "Win" => new List<string> { "Win", "LWin", "RWin" },
            _ => new List<string> { modifier }
        };
    }

    private void ClearPendingTrigger()
    {
        _pendingRule = null;
        _pendingComboKeys = null;
        _releasedKeys.Clear();
    }

    public void Dispose()
    {
        Stop();
    }
}
