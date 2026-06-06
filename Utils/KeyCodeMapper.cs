using System.Runtime.InteropServices;

namespace ContextKeys.Utils;

public static class KeyCodeMapper
{
    private static readonly Dictionary<string, byte> KeyToVk = new(StringComparer.OrdinalIgnoreCase)
    {
        // Letters
        {"A", 0x41}, {"B", 0x42}, {"C", 0x43}, {"D", 0x44}, {"E", 0x45},
        {"F", 0x46}, {"G", 0x47}, {"H", 0x48}, {"I", 0x49}, {"J", 0x4A},
        {"K", 0x4B}, {"L", 0x4C}, {"M", 0x4D}, {"N", 0x4E}, {"O", 0x4F},
        {"P", 0x50}, {"Q", 0x51}, {"R", 0x52}, {"S", 0x53}, {"T", 0x54},
        {"U", 0x55}, {"V", 0x56}, {"W", 0x57}, {"X", 0x58}, {"Y", 0x59}, {"Z", 0x5A},
        // Digits
        {"0", 0x30}, {"1", 0x31}, {"2", 0x32}, {"3", 0x33}, {"4", 0x34},
        {"5", 0x35}, {"6", 0x36}, {"7", 0x37}, {"8", 0x38}, {"9", 0x39},
        // Function keys
        {"F1", 0x70}, {"F2", 0x71}, {"F3", 0x72}, {"F4", 0x73}, {"F5", 0x74},
        {"F6", 0x75}, {"F7", 0x76}, {"F8", 0x77}, {"F9", 0x78}, {"F10", 0x79},
        {"F11", 0x7A}, {"F12", 0x7B}, {"F13", 0x7C}, {"F14", 0x7D}, {"F15", 0x7E},
        {"F16", 0x7F}, {"F17", 0x80}, {"F18", 0x81}, {"F19", 0x82}, {"F20", 0x83},
        {"F21", 0x84}, {"F22", 0x85}, {"F23", 0x86}, {"F24", 0x87},
        // Navigation
        {"Up", 0x26}, {"Down", 0x28}, {"Left", 0x25}, {"Right", 0x27},
        {"Home", 0x24}, {"End", 0x23}, {"PageUp", 0x21}, {"PageDown", 0x22},
        // Edit
        {"Insert", 0x2D}, {"Delete", 0x2E}, {"Backspace", 0x08},
        // Control
        {"Tab", 0x09}, {"CapsLock", 0x14}, {"Escape", 0x1B}, {"Esc", 0x1B},
        {"Enter", 0x0D}, {"Space", 0x20},
        // Modifiers
        {"Shift", 0x10}, {"Ctrl", 0x11}, {"Control", 0x11}, {"Alt", 0x12},
        {"Win", 0x5B}, {"LWin", 0x5B}, {"RWin", 0x5C},
        {"LShift", 0xA0}, {"RShift", 0xA1},
        {"LCtrl", 0xA2}, {"RCtrl", 0xA3},
        {"LAlt", 0xA4}, {"RAlt", 0xA5},
        // Symbols
        {"Minus", 0xBD}, {"Equal", 0xBB},
        {"LeftBracket", 0xDB}, {"RightBracket", 0xDD},
        {"Backslash", 0xDC}, {"Semicolon", 0xBA},
        {"Quote", 0xDE}, {"Comma", 0xBC},
        {"Period", 0xBE}, {"Slash", 0xBF},
        {"Backtick", 0xC0},
        // Numpad
        {"Num0", 0x60}, {"Num1", 0x61}, {"Num2", 0x62}, {"Num3", 0x63},
        {"Num4", 0x64}, {"Num5", 0x65}, {"Num6", 0x66}, {"Num7", 0x67},
        {"Num8", 0x68}, {"Num9", 0x69},
        {"NumAdd", 0x6B}, {"NumSubtract", 0x6D}, {"NumMultiply", 0x6A},
        {"NumDivide", 0x6F}, {"NumDecimal", 0x6E}, {"NumEnter", 0x0D},
        {"NumLock", 0x90},
        // Print/Screen
        {"PrintScreen", 0x2C}, {"ScrollLock", 0x91}, {"Pause", 0x13},
        // Media keys (mapped to virtual codes - hardware-dependent)
        {"VolumeUp", 0xAF}, {"VolumeDown", 0xAE}, {"VolumeMute", 0xAD},
        {"MediaPlayPause", 0xB3}, {"MediaNextTrack", 0xB0},
        {"MediaPreviousTrack", 0xB1}, {"MediaStop", 0xB2},
    };

    private static readonly Dictionary<byte, string> VkToKey = new();

    static KeyCodeMapper()
    {
        foreach (var kvp in KeyToVk)
        {
            if (!VkToKey.ContainsKey(kvp.Value))
                VkToKey[kvp.Value] = kvp.Key;
        }
    }

    public static byte GetVkCode(string keyName)
    {
        return KeyToVk.TryGetValue(keyName, out var vk) ? vk : (byte)0;
    }

    public static string GetKeyName(byte vkCode)
    {
        return VkToKey.TryGetValue(vkCode, out var name) ? name : $"VK({vkCode})";
    }

    public static string GetKeyName(byte vkCode, bool isExtended)
    {
        if (vkCode == 0x0D && isExtended)
            return "NumEnter";

        return GetKeyName(vkCode);
    }

    public static bool IsModifier(byte vkCode)
    {
        return vkCode is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C
            or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5;
    }

    public static bool IsModifier(string keyName)
    {
        return keyName is "Shift" or "Ctrl" or "Control" or "Alt" or "Win"
            or "LShift" or "RShift" or "LCtrl" or "RCtrl" or "LAlt" or "RAlt"
            or "LWin" or "RWin";
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static ushort GetScanCode(byte vkCode)
    {
        return (ushort)MapVirtualKey(vkCode, 0);
    }

    public static bool IsExtendedKey(byte vkCode)
    {
        return vkCode is 0x26 or 0x28 or 0x25 or 0x27  // arrows
            or 0x21 or 0x22 or 0x24 or 0x23           // pgup/pgdn/home/end
            or 0x2D or 0x2E                           // ins/del
            or 0x6F                                    // numpad divide
            or 0xA3 or 0xA5                            // right ctrl/alt
            or 0x5B or 0x5C;                           // left/right windows
    }

    /// <summary>
    /// Get the "base" vk code for a modifier. LShift/RShift → Shift, etc.
    /// </summary>
    public static byte GetBaseModifierVk(string modifierName)
    {
        return modifierName.ToLowerInvariant() switch
        {
            "shift" or "lshift" or "rshift" => 0x10,
            "ctrl" or "control" or "lctrl" or "rctrl" => 0x11,
            "alt" or "lalt" or "ralt" => 0x12,
            "win" or "lwin" or "rwin" => 0x5B,
            _ => 0
        };
    }
}
