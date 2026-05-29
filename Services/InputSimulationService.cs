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
                    ExecuteSequence(action.Keys ?? new List<string>(), action.IntervalMs > 0 ? action.IntervalMs : (globalIntervalMs ?? _defaultIntervalMs));
                    break;
                case "chord":
                    ExecuteChord(action.Keys ?? new List<string>());
                    break;
                case "delay":
                    if (action.Milliseconds > 0)
                        Thread.Sleep(action.Milliseconds);
                    break;
            }
        }
    }

    private static void ExecuteSequence(List<string> keys, int intervalMs)
    {
        foreach (var key in keys)
        {
            var vk = KeyCodeMapper.GetVkCode(key);
            if (vk == 0)
            {
                Logger.Warn($"未知按键: {key}");
                continue;
            }

            SendKey(vk, down: true, key);
            Thread.Sleep(intervalMs);
            SendKey(vk, down: false, key);
            Thread.Sleep(1);
        }
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

    /// <summary>
    /// Send a single key event using keybd_event (simple, reliable API).
    /// Falls back to SendInput if keybd_event doesn't work.
    /// </summary>
    private static void SendKey(byte vkCode, bool down, string keyName)
    {
        byte scan = (byte)KeyCodeMapper.GetScanCode(vkCode);
        var flags = down
            ? (KeyCodeMapper.IsExtendedKey(vkCode) ? Win32Api.KEYEVENTF_EXTENDEDKEY : 0)
            : Win32Api.KEYEVENTF_KEYUP | (KeyCodeMapper.IsExtendedKey(vkCode) ? Win32Api.KEYEVENTF_EXTENDEDKEY : 0);

        try
        {
            Win32Api.keybd_event(vkCode, scan, flags, Win32Api.INJECTED_BY_APP);
            Logger.Info($"keybd_event {(down ? "DOWN" : "UP  ")} VK:{vkCode:X2} Key:{keyName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"keybd_event 失败: {ex.Message}，尝试 SendInput");

            // Fallback to SendInput
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
            Logger.Info($"SendInput fallback Sent:{sent}");
            if (sent == 0)
            {
                var err = Marshal.GetLastWin32Error();
                Logger.Error($"SendInput 失败, LastError:{err}");
            }
        }
    }
}
