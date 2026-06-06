using System.Runtime.InteropServices;
using ContextKeys.Models;
using ContextKeys.Utils;

namespace ContextKeys.Services;

public class InputSimulationService
{
    private readonly int _defaultIntervalMs;

    public InputSimulationService(int defaultIntervalMs = 30)
    {
        _defaultIntervalMs = defaultIntervalMs;
    }

    public void ExecuteActions(List<ActionStep> actions, int? globalIntervalMs = null)
    {
        // Small pre-delay to let the hook suppression settle before injecting new keys
        Thread.Sleep(50);

        foreach (var action in actions)
        {
            switch (action.Type)
            {
                case "sequence":
                    ExecuteSequence(action.Keys ?? new List<string>(), ResolveInterval(action.IntervalMs, globalIntervalMs));
                    break;
                case "chord":
                    ExecuteChord(action.Keys ?? new List<string>());
                    break;
                case "delay":
                    if (action.Milliseconds > 0)
                        Thread.Sleep(ClampDelay(action.Milliseconds));
                    break;
            }
        }
    }

    private int ResolveInterval(int actionIntervalMs, int? globalIntervalMs)
    {
        var interval = actionIntervalMs > 0 ? actionIntervalMs : globalIntervalMs ?? _defaultIntervalMs;
        return Math.Clamp(interval, 1, 10_000);
    }

    private static int ClampDelay(int milliseconds)
    {
        return Math.Clamp(milliseconds, 0, 60_000);
    }

    private static void ExecuteSequence(List<string> keys, int intervalMs)
    {
        var pendingModifiers = new List<(byte Vk, string Key)>();

        foreach (var key in keys)
        {
            var vk = KeyCodeMapper.GetVkCode(key);
            if (vk == 0)
            {
                Logger.Warn($"未知按键: {key}");
                continue;
            }

            if (KeyCodeMapper.IsModifier(key))
            {
                if (!pendingModifiers.Any(modifier => modifier.Vk == vk))
                {
                    SendKey(vk, down: true, key);
                    pendingModifiers.Add((vk, key));
                }
                continue;
            }

            try
            {
                SendKey(vk, down: true, key);
                Thread.Sleep(intervalMs);
                SendKey(vk, down: false, key);
            }
            finally
            {
                ReleasePendingModifiers(pendingModifiers);
            }

            Thread.Sleep(1);
        }

        ReleasePendingModifiers(pendingModifiers);
    }

    private static void ReleasePendingModifiers(List<(byte Vk, string Key)> pendingModifiers)
    {
        for (var index = pendingModifiers.Count - 1; index >= 0; index--)
            SendKey(pendingModifiers[index].Vk, down: false, pendingModifiers[index].Key);

        pendingModifiers.Clear();
    }

    private static void ExecuteChord(List<string> keys)
    {
        if (keys.Count == 0)
            return;

        // Press all keys in order
        foreach (var key in keys)
        {
            var vk = KeyCodeMapper.GetVkCode(key);
            if (vk != 0)
                SendKey(vk, down: true, key);
        }

        Thread.Sleep(30);

        // Release all keys in reverse order
        for (int i = keys.Count - 1; i >= 0; i--)
        {
            var vk = KeyCodeMapper.GetVkCode(keys[i]);
            if (vk != 0)
                SendKey(vk, down: false, keys[i]);
        }
    }

    private static void SendKey(byte vkCode, bool down, string keyName)
    {
        var scan = KeyCodeMapper.GetScanCode(vkCode);
        var extendedKeyFlag = KeyCodeMapper.IsExtendedKey(vkCode) || string.Equals(keyName, "NumEnter", StringComparison.OrdinalIgnoreCase)
            ? Win32Api.KEYEVENTF_EXTENDEDKEY
            : 0;
        var flags = down
            ? extendedKeyFlag
            : Win32Api.KEYEVENTF_KEYUP | extendedKeyFlag;

        var input = new Win32Api.INPUT
        {
            type = Win32Api.INPUT_KEYBOARD,
            u = new Win32Api.InputUnion
            {
                ki = new Win32Api.KEYBDINPUT
                {
                    wVk = vkCode,
                    wScan = scan,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = Win32Api.INJECTED_BY_APP
                }
            }
        };

        var sent = Win32Api.SendInput(1, new[] { input }, Win32Api.InputCbSize);
        Logger.Info($"SendInput {(down ? "DOWN" : "UP  ")} VK:{vkCode:X2} Key:{keyName} Sent:{sent}");
        if (sent == 0)
        {
            var err = Marshal.GetLastWin32Error();
            Logger.Error($"SendInput 失败, LastError:{err}, VK:{vkCode:X2}, Key:{keyName}");
        }
    }
}
