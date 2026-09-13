using Wctl.Platform;

namespace Wctl.Commands.Apps;

/// <summary>
/// Finds an installed app by what the user typed. Tried in order: exact name, exe name, name starts with, name contains.
/// The first level with a hit wins. Hits with different names on the same level are an error, not a guess.
/// </summary>
internal static class AppMatcher
{
    public static AppEntry? Find(IReadOnlyList<AppEntry> apps, string query)
    {
        query = query.Trim();
        if (query.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            query = query[..^4];
        }

        if (query.Length == 0)
        {
            return null;
        }

        Func<AppEntry, bool>[] levels =
        [
            a => a.Name.Equals(query, StringComparison.OrdinalIgnoreCase),
            a => a.Executable.Equals(query, StringComparison.OrdinalIgnoreCase),
            a => a.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase),
            a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase),
        ];

        foreach (var level in levels)
        {
            var hits = apps.Where(level).ToList();
            if (hits.Count == 0)
            {
                continue;
            }

            var names = hits.Select(h => h.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (names.Count > 1)
            {
                var shown = string.Join(", ", names.Take(8)) + (names.Count > 8 ? ", ..." : string.Empty);
                throw new WctlException($"'{query}' matches more than one app: {shown}. Be more specific.");
            }

            return hits[0];
        }

        return null;
    }
}
