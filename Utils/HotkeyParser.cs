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
    /// Check if two hotkeys can resolve to the same runtime trigger.
    /// </summary>
    public static bool AreEqual(string key1, List<string> mods1, string key2, List<string> mods2)
    {
        if (!string.Equals(key1, key2, StringComparison.OrdinalIgnoreCase))
            return false;

        var signatures1 = BuildModifierSignatures(mods1);
        var signatures2 = BuildModifierSignatures(mods2);

        return signatures1.Overlaps(signatures2);
    }

    private static HashSet<string> BuildModifierSignatures(List<string> modifiers)
    {
        var modifierVariants = modifiers.Select(GetModifierAliases).ToList();
        var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (modifierVariants.Count == 0)
        {
            signatures.Add(string.Empty);
            return signatures;
        }

        foreach (var expandedModifiers in ExpandModifierAliases(modifierVariants, 0, new List<string>()))
            signatures.Add(BuildModifierSignature(expandedModifiers));

        return signatures;
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

    private static string BuildModifierSignature(List<string> modifiers)
    {
        return string.Join("+", modifiers.Select(m => m.ToLowerInvariant()).OrderBy(m => m));
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
}
