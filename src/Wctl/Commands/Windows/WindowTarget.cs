using System.Globalization;
using Wctl.Platform;

namespace Wctl.Commands.Windows;

/// <param name="Windows">Matching windows, frontmost first.</param>
/// <param name="ByApp">True when the query named an app, so the match means "all windows of that app".</param>
internal sealed record WindowMatch(IReadOnlyList<WindowInfo> Windows, bool ByApp)
{
    public WindowInfo First => Windows[0];

    /// <summary>What to call the target in output: the app name.</summary>
    public string Label => First.App;
}

/// <summary>
/// Turns what the user typed into windows. Tried in order: number from 'w ls', exact app name,
/// part of an app name, part of a window title. The first level with a hit wins.
/// </summary>
internal static class WindowTarget
{
    public static WindowMatch Resolve(IReadOnlyList<WindowInfo> windows, string query)
        => Find(windows, query) ?? throw new WctlException($"No window matches '{query}'. Run 'w ls' to see open windows.");

    /// <returns>Null when nothing matches. Throws for a bad number or an ambiguous query.</returns>
    public static WindowMatch? Find(IReadOnlyList<WindowInfo> windows, string query)
    {
        query = query.Trim();
        if (int.TryParse(query, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            if (number < 1 || number > windows.Count)
            {
                throw new WctlException(windows.Count == 0
                    ? "There are no windows to pick from."
                    : $"There is no window number {number}. Run 'w ls' to see the numbers.");
            }

            return new WindowMatch([windows[number - 1]], ByApp: false);
        }

        if (query.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            query = query[..^4];
        }

        if (query.Length == 0)
        {
            throw new WctlException("Which window? Give a number from 'w ls', an app name or part of a title.");
        }

        var exact = windows.Where(w => w.App.Equals(query, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count > 0)
        {
            return new WindowMatch(exact, ByApp: true);
        }

        var partial = windows.Where(w => w.App.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        if (partial.Count > 0)
        {
            var apps = partial.Select(w => w.App).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (apps.Count > 1)
            {
                throw new WctlException($"'{query}' matches more than one app: {string.Join(", ", apps)}. Be more specific.");
            }

            return new WindowMatch(partial, ByApp: true);
        }

        var byTitle = windows.Where(w => w.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byTitle.Count == 0)
        {
            return null;
        }

        var titleApps = byTitle.Select(w => w.App).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (titleApps.Count > 1)
        {
            var candidates = byTitle.Take(5).Select(w => $"{w.App}: {w.Title}");
            throw new WctlException($"'{query}' matches windows of more than one app: {string.Join("; ", candidates)}. Use the number from 'w ls'.");
        }

        return new WindowMatch([byTitle[0]], ByApp: false);
    }
}
