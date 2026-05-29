using ContextKeys.Models;

namespace ContextKeys.Services;

public class ProfileMatchService
{
    /// <summary>
    /// Match a foreground window against all enabled profiles.
    /// Returns the first matching profile, or null.
    /// </summary>
    public static Profile? Match(string processName, string title, List<Profile> profiles)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;

        foreach (var profile in profiles)
        {
            if (!profile.Enabled)
                continue;

            var match = profile.Match;
            if (match == null)
                continue;

            switch (match.MatchMode)
            {
                case "process_only":
                    if (string.Equals(processName, match.ProcessName, StringComparison.OrdinalIgnoreCase))
                        return profile;
                    break;

                case "process_and_title_contains":
                    if (string.IsNullOrWhiteSpace(match.TitleContains))
                        goto case "process_only";

                    if (string.Equals(processName, match.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                        title.Contains(match.TitleContains, StringComparison.OrdinalIgnoreCase))
                        return profile;
                    break;

                case "process_and_title_equals":
                    if (string.IsNullOrWhiteSpace(match.TitleEquals))
                        goto case "process_only";

                    if (string.Equals(processName, match.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(title, match.TitleEquals, StringComparison.OrdinalIgnoreCase))
                        return profile;
                    break;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a profile matches the given window.
    /// </summary>
    public static bool IsMatch(Profile profile, string processName, string title)
    {
        var match = profile.Match;
        if (match == null || !profile.Enabled)
            return false;

        return match.MatchMode switch
        {
            "process_only" => string.Equals(processName, match.ProcessName, StringComparison.OrdinalIgnoreCase),
            "process_and_title_contains" =>
                string.Equals(processName, match.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(match.TitleContains) || title.Contains(match.TitleContains, StringComparison.OrdinalIgnoreCase)),
            "process_and_title_equals" =>
                string.Equals(processName, match.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(title, match.TitleEquals, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
