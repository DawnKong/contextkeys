namespace ContextKeys.Utils;

public static class HotkeyParser
{
    /// <summary>
    /// Parse a display string like "Ctrl+Alt+J" into key + modifiers.
    /// </summary>
    public static (string key, List<string> modifiers) Parse(string display)
    {
        var modifiers = new List<string>();
        var parts = display.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string key = string.Empty;
        foreach (var part in parts)
        {
            if (KeyCodeMapper.IsModifier(part))
            {
                modifiers.Add(part);
            }
            else
            {
                key = part;
            }
        }

        return (key, modifiers);
    }

    /// <summary>
    /// Build display string from key + modifiers.
    /// </summary>
    public static string BuildDisplay(string key, List<string> modifiers)
    {
        if (modifiers.Count == 0)
            return key;
        return string.Join(" + ", modifiers.Concat(new[] { key }));
    }

    /// <summary>
    /// Check if two keys are the same (ignoring order of modifiers).
    /// </summary>
    public static bool AreEqual(string key1, List<string> mods1, string key2, List<string> mods2)
    {
        if (!string.Equals(key1, key2, StringComparison.OrdinalIgnoreCase))
            return false;
        if (mods1.Count != mods2.Count)
            return false;

        var sorted1 = mods1.Select(m => m.ToLowerInvariant()).OrderBy(m => m).ToList();
        var sorted2 = mods2.Select(m => m.ToLowerInvariant()).OrderBy(m => m).ToList();

        return sorted1.SequenceEqual(sorted2);
    }
}
