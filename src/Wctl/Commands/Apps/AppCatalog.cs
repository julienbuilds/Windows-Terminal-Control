using System.Globalization;
using System.Text;
using Wctl.Platform;

namespace Wctl.Commands.Apps;

/// <summary>
/// The list of installed apps, cached in a file. Asking Windows takes about 800 ms, which is too slow for a tool
/// whose point is being instant, so the answer is kept for a day. A lookup that finds nothing refreshes once,
/// so an app installed today is found without anyone thinking about caches.
/// </summary>
internal sealed class AppCatalog(IShell shell, ITextStore cache, IClock clock)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    private IReadOnlyList<AppEntry>? apps;
    private bool fromCache;

    public IReadOnlyList<AppEntry> Apps => apps ??= Load();

    public IReadOnlyList<AppEntry> Refresh()
    {
        var fresh = shell.InstalledApps();
        cache.Write(Format(fresh, clock.Now));
        apps = fresh;
        fromCache = false;
        return fresh;
    }

    /// <summary>The app the user means, or null. Refreshes the cache once before giving up.</summary>
    public AppEntry? Find(string query)
    {
        var hit = AppMatcher.Find(Apps, query);
        return hit is null && fromCache ? AppMatcher.Find(Refresh(), query) : hit;
    }

    internal static string Format(IEnumerable<AppEntry> apps, DateTimeOffset written)
    {
        var sb = new StringBuilder();
        sb.Append("# wctl app cache, written ").Append(written.ToString("O", CultureInfo.InvariantCulture)).Append('\n');
        foreach (var app in apps)
        {
            if (app.Name.Contains('\t', StringComparison.Ordinal) || app.Id.Contains('\t', StringComparison.Ordinal))
            {
                continue; // A tab would break the line. No real app name has one, and the refresh on miss covers it.
            }

            sb.Append(app.Name).Append('\t').Append(app.Id).Append('\t').Append(app.Executable).Append('\n');
        }

        return sb.ToString();
    }

    /// <returns>The cached apps, or null when the file is missing, too old or not readable.</returns>
    internal static List<AppEntry>? TryParse(string? text, DateTimeOffset now, TimeSpan maxAge)
    {
        var lines = text?.Split('\n');
        if (lines is null || lines.Length == 0)
        {
            return null;
        }

        var stamp = lines[0].Split("written ");
        if (stamp.Length != 2
            || !DateTimeOffset.TryParse(stamp[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var written)
            || now - written > maxAge
            || written > now + TimeSpan.FromMinutes(5))
        {
            return null;
        }

        var apps = new List<AppEntry>(lines.Length);
        foreach (var line in lines.Skip(1))
        {
            var parts = line.TrimEnd('\r').Split('\t');
            if (parts.Length == 3 && parts[0].Length > 0)
            {
                apps.Add(new AppEntry(parts[0], parts[1], parts[2]));
            }
        }

        return apps.Count > 0 ? apps : null;
    }

    private IReadOnlyList<AppEntry> Load()
    {
        var cached = TryParse(cache.Read(), clock.Now, MaxAge);
        if (cached is null)
        {
            return Refresh();
        }

        fromCache = true;
        return cached;
    }
}
